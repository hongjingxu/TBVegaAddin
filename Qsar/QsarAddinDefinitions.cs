using System;
using System.Collections.Generic;
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
            "Endpoint comment",
            "Test type",
            "Sex",
            "Route of administration",
            "Organ",
            "Gene name",
            "Strain",
            "Metabolic activation",
            "Test specificity",
            "Test condition",
            "Type of method",
            "Assay provider",
            "Test guideline",
            "Reference",
            "Reference Link",
            "n(Train)",
            "n(Invisible training)",
            "n(Internal Valid)",
            "n(Calibration)",
            "n(Test)",
            "R2(Train)",
            "RMSE(Train)",
            "R2adj(Train)",
            "Q2(Train)",
            "Fisher(Train)",
            "S(Train)",
            "Sdev(Train)",
            "SSR(Train)",
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
            "R2adj(Test)",
            "Fisher(Test)",
            "S(Test)",
            "Sdev(Test)",
            "SSR(Test)",
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
            if (Modelinfo["Effect"] != "")
            {
                dict.Add("Effect", Modelinfo["Effect"]);
            }

            if (Modelinfo["Test organisms (species)"] != "")
            {
                dict.Add("Test organisms (species)", Modelinfo["Test organisms (species)"]);
            }
            if (Modelinfo["QMRFlink"] != "null")
            {
                dict.Add("QMRF", Modelinfo["QMRFlink"]);
            }
            foreach (string Colname in Metadatalist)
                if (Modelinfo[Colname] != "")
                {
                    dict.Add(Colname, Modelinfo[Colname]);
                }

            return dict;
        }



        //works only with a training set
        public static TbQsarStatistics M4RatioModelStatistics
        {
            get
            {
                return new TbQsarStatistics(new int?(0), new int?(0), new int?(0), new int?(0));
            }
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
               /*helpFile*/ System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("VegaAddins.dll", "guide/" + Modelinfo["GuideUrl"]),
            /*additionalInfo */
            (IEnumerable<TbObjectAboutTextPair>)new TbObjectAboutTextPair[3]
            {
        new TbObjectAboutTextPair("Adopted", "Toolbox 4.4. November 2019"),
        new TbObjectAboutTextPair("Documentation", System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("VegaAddins.dll", "guide/" + Modelinfo["GuideUrl"])),
        new TbObjectAboutTextPair("QMRF", Modelinfo["QMRFlink"])
            }) ;
        }
    }
}
