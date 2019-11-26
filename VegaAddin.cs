using insilico.vega.vegadockcli;
using net.sf.jni4net;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Toolbox.Docking.Api;
using Toolbox.Docking.Api.Objects;
using VegaAddins.Qsar;

namespace VegaAddins
{
    public class VegaAddin : IToolboxAddin
    {

        public IEnumerable<ITbObjectFactory> GetToolboxObjectFactories()
        {
            List<Dictionary<string, string>> VegaModels = RetrieveModelInfo(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            List<ITbObjectFactory> tbObjectFactoryList = new List<ITbObjectFactory>();
            foreach (Dictionary<string, string> model in VegaModels.Where(f => f["UnitFamily"]!=""))

                //EXCLUDE CLASSIFICATION MODELS

                    tbObjectFactoryList.Add((ITbObjectFactory)new TbQsarAddinFactory(model));

            return (IEnumerable<ITbObjectFactory>)tbObjectFactoryList;

        }
        private List<Dictionary<string, string>> RetrieveModelInfo(string path)
        {
            string csvpath = path + "/Vega Models.txt";
            //retrieve from CSV
            var lines = File.ReadAllLines(csvpath);

            //1. Read all headers
            string[] columnHeaders = lines[0].Split('\t');

            //2. Instantiate your end result variable.
            List<Dictionary<string, string>> Models = new List<Dictionary<string, string>>();

            ////3. create the brigde between java and cr to get more info N.B. THIS IS IF YOU WANT TO EXTRACT INFO FROM INSILICOCORE
            //// TODO: add additional info taken from the models
            //var setup = new BridgeSetup(true);
            //setup.AddAllJarsClassPath(path);
            //Bridge.CreateJVM(setup);
            //Bridge.RegisterAssembly(typeof(VegaDockInfo).Assembly);
            //VegaDockInfo vdi = new VegaDockInfo();


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
                Models.Add(newDict);

            }
            return Models;
        }
    }
   
    }

