﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geoprocessing;

namespace GAWetlands
{
    public class QueryHelperClass
    {
        protected string[] fields = null;

        public System.Collections.Generic.List<string> LastQueryStrings = new System.Collections.Generic.List<string>();

        protected static string GetAssemblyPath()
        {
            var codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            var uriBuilder = new UriBuilder(codeBase);
            var asmPath = Uri.UnescapeDataString(uriBuilder.Path);
            asmPath = System.IO.Path.GetDirectoryName(asmPath);
            return asmPath;
        }

        public ICursor doQueryItems(string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return null;
            }

            try
            {
                IFeatureLayer2 ifl2 = (IFeatureLayer2)ArcMap.Document.SelectedLayer;
                IGeoFeatureLayer igfl = (IGeoFeatureLayer)ifl2;
                ITable tbl = (ITable)((IFeatureLayer)igfl).FeatureClass;
                IQueryFilter qf = getQueryFilter(values);

                LastQueryStrings.Clear();
                LastQueryStrings.Add(qf.WhereClause);

                IDataStatistics ids = new DataStatisticsClass();
                return tbl.Search(qf, false);
            }
            catch (Exception err)
            {
                System.Windows.Forms.MessageBox.Show("An error occurred during the query. Ensure the selected layer is an NWI or NWI+ layer and try again.", "Error");
            }
            finally
            {
            }

            return null;
        }

        public virtual IQueryFilter getQueryFilter(string[] values)
        {
            IQueryFilter qf = new QueryFilterClass();

            for (int ii = 0; ii < values.Length; ii++)
            {
                if (fields.Length > 1)
                {
                    string[] curr_values = values[ii].Split(';');

                    qf.WhereClause += " (";

                    for (int j = 0; j < fields.Length; j++)
                    {
                        qf.WhereClause = qf.WhereClause + fields[j] + "='" + curr_values[j].Trim() +"' ";

                        if (j < fields.Length - 1)
                        {
                            qf.WhereClause += " AND ";
                        }
                    }

                    qf.WhereClause += ") ";
                }
                else
                {
                    qf.WhereClause = qf.WhereClause + " " + fields[0] + "='" + values[ii] + "'";// +((ii <= (values.Length - 1)) ? "" : "OR ");
                }

                if (ii < (values.Length - 1))
                    qf.WhereClause += " OR ";
            }
            return qf;
        }

        public virtual string[] getQueryValues(ListBox lb)
        {
            ListBox.SelectedObjectCollection soc = lb.SelectedItems;
            string[] queryValues = new string[soc.Count];

            int i = 0;

            foreach (string curr in soc)
            {
                char[] splits = { '(', ')' };
                string value = curr.Split(splits)[1];
                queryValues[i] = value;
                i++;
            }
            return queryValues;
        }

        public virtual string[] getQueryValueOptions(string fieldname, string filename)
        {
            ESRI.ArcGIS.Carto.ILayerFile layerFile = new LayerFileClass();
            //layerFile.Open("\\\\tornado\\Research3\\Tony\\Wetlands\\wetlands10.1\\10.0\\" + queryType + "_Poly.lyr");
            layerFile.Open(GetAssemblyPath() + "\\Symbology\\" + filename + ".lyr");

            IGeoFeatureLayer igfl_lyr = (IGeoFeatureLayer)layerFile.Layer;
            IUniqueValueRenderer iuvr = (IUniqueValueRenderer)igfl_lyr.Renderer;

            fields = new string[]{ fieldname };

            if (iuvr.FieldCount == 1)
            {
                string[] s = new string[iuvr.ValueCount];

                for (int j = 0; j < iuvr.ValueCount; j++)
                {
                    s[j] = iuvr.Label[iuvr.Value[j]] + " (" + iuvr.Value[j] + ")";
                }

                return s;
            }
            else
            {
                string[] s = new string[iuvr.ValueCount];
                string WhereClause = "";

                char[] delimiter = { iuvr.FieldDelimiter[0] };

                string prefix = ""; //sql.GetSpecialCharacter(esriSQLSpecialCharacters.esriSQL_DelimitedIdentifierPrefix);
                string suffix = ""; //'sql.GetSpecialCharacter(esriSQLSpecialCharacters.esriSQL_DelimitedIdentifierSuffix);

                List<string> l = new List<string>();

                for (int j = 0; j < iuvr.ValueCount; j++ )
                {
                    string[] currValues = iuvr.Value[j].Split(delimiter);

                    s[j] = iuvr.Label[iuvr.Value[j]] + "( " + string.Join(";", currValues) + " )";
                }

                for (int k = 0; k < iuvr.FieldCount; k++)
                {
                    l.Add(iuvr.Field[k]);
                }

                fields = l.ToArray();
#if false

                for (int j = 0; j < iuvr.ValueCount; j++)
                {
                    WhereClause = "";
                    string[] currValues = iuvr.Value[j].Split(delimiter);

                    for (int k = 0; k < currValues.Length; k++)
                    {
                        if (k > 0)
                            WhereClause += " AND ";

                        WhereClause += prefix + iuvr.Field[k] + suffix + " = '" + currValues[k].Trim() + "'";
                    }

                    s[j] = iuvr.Label[iuvr.Value[j]] + " (" + WhereClause + ")";
                }
#endif
                return s;
            }
        }
    }

    class CSVHelper
    {
        public static System.Collections.Generic.List<System.Collections.ArrayList> readCSV(string filename)
        {
            Microsoft.VisualBasic.FileIO.TextFieldParser tfp = new Microsoft.VisualBasic.FileIO.TextFieldParser(filename);
            tfp.Delimiters = new string[] { "," };
            tfp.HasFieldsEnclosedInQuotes = false;

            List<System.Collections.ArrayList> a = new List<System.Collections.ArrayList>();

            try
            {
                while (!tfp.EndOfData)
                {
                    string[] s = tfp.ReadFields();
                    a.Add(new System.Collections.ArrayList(s));
                }
            }
            catch (Exception err)
            {
            }
            finally
            {
                tfp.Close();
            }

            return a;
        }
    }

    class QueryHelperSubclass : QueryHelperClass
    {
        public QueryHelperSubclass()
        {
        }

        public override string[] getQueryValueOptions(string fieldname, string filename)
        {
            System.Collections.Generic.List<System.Collections.ArrayList> valuesAndLabels = null;

            //valuesAndLabels = CSVHelper.readCSV("\\\\tornado\\Research3\\Tony\\Wetlands\\wetlands10.1\\10.0\\Subclass.csv");
            valuesAndLabels = CSVHelper.readCSV(GetAssemblyPath() + "\\Symbology\\Subclass.csv");
            string[] values = new string[valuesAndLabels.Count];

            for (int i = 0; i < valuesAndLabels.Count; i++)
            {
                values[i] = valuesAndLabels[i][2] + " (" + valuesAndLabels[i][0] + "; " + valuesAndLabels[i][1] + ")";
            }

            fields = new string[] { fieldname };

            return values;
        }

        public override IQueryFilter getQueryFilter(string[] values)
        {
            IQueryFilter qf = new QueryFilterClass();

            for (int ii = 0; ii < values.Length; ii++)
            {
                qf.WhereClause = qf.WhereClause + values[ii];

                if (ii < (values.Length - 1))
                    qf.WhereClause += "OR ";
            }
            return qf;
        }

        public override string[] getQueryValues(ListBox lb)
        {
            ListBox.SelectedObjectCollection soc = lb.SelectedItems;
            string[] queryValues = new string[soc.Count];

            int i = 0;

            foreach (string curr in soc)
            {
                char[] splits = { '(', ';', ')' };
                string value = null;

                if (curr.Contains("All"))
                {
                    if (curr.Contains("Subtidal"))
                    {
                        value = "( System <> 'L' AND Subsystem = '1')";
                    }
                    else
                    {
                        value = "( System <> 'L' AND Subsystem = '2')";
                    }
                }
                else
                {
                    string[] vals = curr.Split(splits);
                    value = "( Class1 = '" + vals[1] + "' AND Subclass1 = '" + vals[2].Trim() + "') ";
                }
                queryValues[i] = value;
                i++;
            }
            return queryValues;
        }
    }

    class QueryHelperSubsystem : QueryHelperClass
    {
        public QueryHelperSubsystem()
        {
        }

        public override string[] getQueryValueOptions(string fieldname, string filename)
        {
            System.Collections.Generic.List<System.Collections.ArrayList> valuesAndLabels = null;
            //valuesAndLabels = CSVHelper.readCSV("\\\\tornado\\Research3\\Tony\\Wetlands\\wetlands10.1\\10.0\\Subsystem.csv");
            valuesAndLabels = CSVHelper.readCSV(GetAssemblyPath() + "\\Symbology\\Subsystem.csv");

            string[] values = new string[valuesAndLabels.Count + 2];

            for (int i = 0; i < valuesAndLabels.Count; i++)
            {
                values[i] = valuesAndLabels[i][2] + " (" + valuesAndLabels[i][0] + "; " + valuesAndLabels[i][1] + ")";
            }

            values[valuesAndLabels.Count] = "All Subtidal (E, M; 1)";
            values[valuesAndLabels.Count + 1] = "All Intertidal (E, M; 2)";

            fields = new string[] { fieldname };

            return values;
        }
    }

    class QueryHelperLLWW : QueryHelperClass
    {
        public override string[] getQueryValueOptions(string queryType, string fileSuffix)
        {
            ESRI.ArcGIS.Carto.ILayerFile layerFile = new LayerFileClass();
            //layerFile.Open("\\\\tornado\\Research3\\Tony\\Wetlands\\wetlands10.1\\10.0\\" + queryType + "_Poly.lyr");
            layerFile.Open(GetAssemblyPath() + "\\Symbology\\NWIPlus" + fileSuffix + ".lyr");

            IGeoFeatureLayer igfl_lyr = (IGeoFeatureLayer)layerFile.Layer;
            IUniqueValueRenderer iuvr = (IUniqueValueRenderer)igfl_lyr.Renderer;

            fields = new string[]{ queryType };

            if (iuvr.FieldCount == 1)
            {
                string[] s = new string[iuvr.ValueCount];

                for (int j = 0; j < iuvr.ValueCount; j++)
                {
                    s[j] = iuvr.Label[iuvr.Value[j]] + " (" + iuvr.Value[j] + ")";
                }

                return s;
            }
            else
            {
                string[] s = new string[iuvr.ValueCount];
                string WhereClause = "";

                char[] delimiter = { iuvr.FieldDelimiter[0] };

                string prefix = ""; //sql.GetSpecialCharacter(esriSQLSpecialCharacters.esriSQL_DelimitedIdentifierPrefix);
                string suffix = ""; //'sql.GetSpecialCharacter(esriSQLSpecialCharacters.esriSQL_DelimitedIdentifierSuffix);

                for (int j = 0; j < iuvr.ValueCount; j++)
                {
                    WhereClause = "";
                    string[] currValues = iuvr.Value[j].Split(delimiter);

                    for (int k = 0; k < currValues.Length; k++)
                    {
                        if (k > 0)
                            WhereClause += " AND ";

                        WhereClause += prefix + iuvr.Field[k] + suffix + " = '" + currValues[k].Trim() + "'";
                    }

                    s[j] = iuvr.Label[ iuvr.Value[j] ] + " (" + WhereClause + ")";
                }
                return s;
            }
        }

        public override IQueryFilter getQueryFilter(string[] values)
        {
            return base.getQueryFilter(values);
        }

        public override string[] getQueryValues(ListBox lb)
        {
            return base.getQueryValues(lb);
        }
    }

#if false
    class QueryHelperLLWW_Modifier : QueryHelperLLWW
    {
        private IUniqueValueRenderer iuvr = null;

        public string[] getQueryValueOptions_withSuffix(string queryType, string fileSuffix)
        {
            ESRI.ArcGIS.Carto.ILayerFile layerFile = new LayerFileClass();
            //layerFile.Open("\\\\tornado\\Research3\\Tony\\Wetlands\\wetlands10.1\\10.0\\" + queryType + "_Poly.lyr");
            layerFile.Open(GetAssemblyPath() + "\\Symbology\\LLWWW_" + queryType + "_Polygon" + fileSuffix + ".lyr");

            IGeoFeatureLayer igfl_lyr = (IGeoFeatureLayer)layerFile.Layer;
            iuvr = (IUniqueValueRenderer)igfl_lyr.Renderer;

            string[] s = new string[iuvr.ValueCount];

            for (int j = 0; j < iuvr.ValueCount; j++)
            {
                s[j] = iuvr.Label[iuvr.Value[j]] + " (" + iuvr.Value[j] + ")";
            }

            return s;
        }

        public override IQueryFilter getQueryFilter(string[] values)
        {
            return base.getQueryFilter(values);
        }

        public override string[] getQueryValues(ListBox lb)
        {
            string[] values = new string[iuvr.ValueCount];

            char[] delimiter = { iuvr.FieldDelimiter[0] };

            for (int j = 0; j < iuvr.ValueCount; j++)
            {
                values[j] = iuvr.Label[ iuvr.Value[j] ] + " (" + iuvr.Value[j] + ")";

/*
                string[] currValues = iuvr.Value[j].Split(delimiter);

                for (int k = 0; k < currValues.Length; k++)
                {
                }

*/
            }

            return values;
        }

    }
#endif
}