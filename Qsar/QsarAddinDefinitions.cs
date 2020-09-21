using insilico.vega.vegadockcli;
using net.sf.jni4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Toolbox.Docking.Api.Chemical;
using Toolbox.Docking.Api.Data;
using Toolbox.Docking.Api.Objects;
using Toolbox.Docking.Api.Objects.Qsar;
using Toolbox.Docking.Api.Units;

namespace VegaAddins.Qsar
{

    public static class QsarAddinDefinitions
    {


        public static Dictionary<string, string> getMetaDataValues(Dictionary<string, string> Modelinfo)
        {
            string[] Metadatalist = new string[] {
                "Effect",
                "Test organisms (species)",
"Endpoint comment",
"Test type",
"Sex",
"Route of administration",
"Organ",
"Strain",
"Metabolic activation",
"Type of method",
"Assay provider",
"Test guideline",
"QMRF",
"Reference",
"Reference Link",
"R2(Train)",
"RMSE(Train)",
"Q2(Train)",
"Fisher(Train)",
"S(Train)",
"R2(Invisible training)",
"RMSE(Invisible training)",
"Q2(Invisible training)",
"Fisher(Invisible training)",
"S(Invisible training)",
"R2(Calibration)",
"RMSE(Calibration)",
"Q2(Calibration)",
"Fisher(Calibration)",
"S(calibration)",
"R2(Test)",
"RMSE(Test)",
"Fisher(Test)",
"S(Test)",
"Accuracy(Train)",
"Specificity(Train)",
"Sensitivity(Train)",
"Accuracy(Internal Valid)",
"Specificity(Internal Valid)",
"Sensitivity(Internal Valid)",
"Accuracy(Test)",
"Specificity(Test)",
"Sensitivity(Test)"
        };
            Dictionary<string, string> dict = new Dictionary<string, string>()
        {
       {
        "Endpoint",
        Modelinfo["Endpoint"]
      } };
            foreach (string Colname in Metadatalist)
                if (Modelinfo[Colname] != "")
                {
                    dict.Add(Colname, Modelinfo[Colname]);
                }

            return dict;
        }

        public static IReadOnlyList<ChemicalWithData> GetSet(Dictionary<string, string> Modelinfo, TbScale ScaleDeclaration, String Set)
        {
            List<ChemicalWithData> SetList = new List<ChemicalWithData>();

            var setup = new BridgeSetup(false);
            //setup.AddAllJarsClassPath(@"B:\ToolboxAddinExamples\lib");

            setup.AddAllJarsClassPath(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            Bridge.CreateJVM(setup);
            Bridge.RegisterAssembly(typeof(ChemicalinSet).Assembly);
            java.util.List list = ChemicalinSet.getDataset(Modelinfo["tag"], Set);

            if (list.size() == 0)
            {
                return SetList;
            }

            for (int i = 0; i < list.size(); i++)
            {

                TbData Mockdescriptordata = new TbData(new TbUnit(TbScale.EmptyRatioScale.FamilyGroup, TbScale.EmptyRatioScale.BaseUnit), new double?());
                ChemicalinSet cur_Chemical = (ChemicalinSet)list.get(i);
                TbData cur_exp = (TbData)Utilities.ConvertData(cur_Chemical.getExperimental(), ScaleDeclaration, Modelinfo);
                SetList.Add(new ChemicalWithData(cur_Chemical.getCAS(), new[] { "N.A." }, cur_Chemical.getSmiles(),
                    new TbDescribedData[] { new TbDescribedData(Mockdescriptordata, null) },
                    new TbDescribedData(cur_exp, null)));
            }
            return SetList;
        }



        //works only with a training set
        public static TbQsarStatistics M4RatioModelStatistics(String tag)
        {
            //Not Working
            var setup = new BridgeSetup(false);
            //setup.AddAllJarsClassPath(@"B:\ToolboxAddinExamples\lib");
            setup.AddAllJarsClassPath(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            Bridge.CreateJVM(setup);
            Bridge.RegisterAssembly(typeof(ChemicalinSet).Assembly);
            int nMolTrain = ChemicalinSet.getnMolTrain(tag);
            int nMolTest = ChemicalinSet.getnMolTest(tag);
            return new TbQsarStatistics(nMolTrain, nMolTrain, nMolTest, nMolTest);

        }

        public static TbObjectAbout GetM4ObjectAbout(Dictionary<string, string> Modelinfo)
        {

            return new TbObjectAbout(
                /*Description*/   Modelinfo["Description(long)"],
               /*donator  */ "Istituto di Ricerche Farmacologiche Mario Negri IRCCS Laboratory of Environmental Chemistry and Toxicology Via Mario Negri 2, 20156 Milan, Italy",
              /* disclaimer*/ "Vega implementation based on version 1.1.5 BETA \nThe application is released under the GNU GPL-3 license\nVega uses the following libraries:\n\tChemistry Development Kit (CDK) ver 1.4.9\n\tiText ver 2.1.4\n\tWeka ver 3.5.8\n\tHttpClient(Apache HttpComponents) ver 4.1.3\n\tjPMML ver 1.3.6",
              /* authors*/ "Istituto di Ricerche Farmacologiche Mario Negri IRCCS Laboratory of Environmental Chemistry and Toxicology Via Mario Negri 2, 20156 Milan, Italy",
              /* url */ "https://www.vegahub.eu/",
              /* name*/ "VEGA - " + Modelinfo["Modelname"],
              "https://www.vegahub.eu/vegahub-dwn/guide/" + Modelinfo["GuideUrl"],
            /*helpFile*/ /*System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("VegaAddins.dll", "guide/" + Modelinfo["GuideUrl"]),*/
            /*additionalInfo */
            (IEnumerable<TbObjectAboutTextPair>)new TbObjectAboutTextPair[3]
            {
        new TbObjectAboutTextPair("Adopted", "Toolbox 4.4. November 2019"),
        new TbObjectAboutTextPair("Documentation", System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("VegaAddins.dll", "guide/" + Modelinfo["GuideUrl"])),
        new TbObjectAboutTextPair("QMRF", Modelinfo["QMRFlink"])
            });
        }
    }


}
