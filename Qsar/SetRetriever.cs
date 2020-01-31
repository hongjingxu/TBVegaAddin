using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Docking.Api.Chemical;
using Toolbox.Docking.Api.Objects;
using Toolbox.Docking.Api.Data;
using Toolbox.Docking.Api.Units;
using System.Globalization;

namespace VegaAddins.Qsar
{
    public static class SetRetriever
        {
        public static IReadOnlyList<ChemicalWithData> getSet(Dictionary<string, string> Modelinfo, TbScale ScaleDeclaration,  string set)
        {
            TbUnit qsarunit = new TbUnit(ScaleDeclaration.Name, Modelinfo["Unit"]);
            List<ChemicalWithData> DataSet = new List<ChemicalWithData>();

            List<Dictionary<string, string>> ChemicalsDict = CSVtoDict( Modelinfo);
            foreach (Dictionary<string, string> ChemicalDict in ChemicalsDict) {

                int Cas = Convert.ToInt32(string.Join(null, System.Text.RegularExpressions.Regex.Split(ChemicalDict["CAS"], "[^\\d]")));
                string name = ChemicalDict["ID"];
                string smiles = ChemicalDict["SMILES"];
                string SetInfo = ChemicalDict["Set"];
                double Experimental = double.Parse(ChemicalDict["Exp"], CultureInfo.InvariantCulture);

                if (SetInfo == set) {
                    ChemicalWithData chemical = new ChemicalWithData(Cas, new[] { name }, smiles,
                                new[] { new TbDescribedData(new TbData(qsarunit, new double?()), null) },
                                new TbDescribedData(new TbData(qsarunit, Experimental), null));

                    DataSet.Add(chemical);
                        }
            }
            return DataSet;
        }
        private static List<Dictionary<string, string>> CSVtoDict( Dictionary<string, string> Modelinfo)
        {


            string tspath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)+ "/Datasets/ts_" + Modelinfo["tag"];

            //retrieve from CSV
            var lines = File.ReadAllLines(tspath);

            //1. Read all headers
            string[] columnHeaders = lines[0].Split('\t');

            //2. Instantiate your end result variable.
            List<Dictionary<string, string>> Chemicals = new List<Dictionary<string, string>>();



            //3. Process all lines (except the header row!)
            foreach (var line in lines.Skip(1))
            {
                //3.1 Instantiate the resulting dictionary
                var newDict = new Dictionary<string, string>();

                //3.2 Split the data
                var cells = line.Split('\t');

                //3.3 Add an entry for each retrieved header.
                for (int i = 0; i < columnHeaders.Length; i++)
                {
                    newDict.Add(columnHeaders[i], cells[i]);
                }
                ////add java additional info
                //vdi.@run(newDict["tag"]);

                //newDict.Add("Description", vdi.getDescription());
                //newDict.Add("DescriptionLong", vdi.getDescriptionLong());
                //newDict.Add("Unit", vdi.getUnit());
                //newDict.Add("GuideURL", vdi.getGuideURL());
                //newDict.Add("QMRFLink", vdi.getQMRFLink());

                //3.4 Add the dictionary to the resulting list
                Chemicals.Add(newDict);

            }
            return Chemicals;
        }
    }
}
}
