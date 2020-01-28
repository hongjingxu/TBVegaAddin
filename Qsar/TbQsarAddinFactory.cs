using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Toolbox.Docking.Api.Chemical;
using Toolbox.Docking.Api.Control;
using Toolbox.Docking.Api.Data;
using Toolbox.Docking.Api.Objects;
using Toolbox.Docking.Api.Objects.Qsar;
using Toolbox.Docking.Api.Units;



namespace VegaAddins.Qsar
{
    public class TbQsarAddinFactory : ITbQsarFactory, ITbObjectFactory, ITbObjectFactoryDomain
    {

        private readonly Dictionary<string, string> Modelinfo;

        public TbObjectFlags Flags { get; }

        public QsarFlags QsarFlags
        {
            get
            {
                return QsarFlags.None;
            }
        }

        public TbObjectId ObjectId { get; }

        public TbObjectAbout ObjectAbout { get; }

        public string ClientDomainExplainer
        {
            get
            {
                return "VEGA ADI";
            }
        }

        public TbMetadata Metadata { get; }

        public string AgreementInfo
        {
            get
            {
                return (string)null;
            }
        }

        public string ReportDisclaimer
        {
            get
            {
                return (string)null;
            }
        }

        public IReadOnlyList<string> EndpointLocation { get; }

        public TbScale ScaleDeclaration { get; }

        public IReadOnlyList<QsarDescriptorInfo> XDescriptors { get; private set; }


        public string QmrfLocation
        {
            //getvega

            //{
            //    if (Modelinfo["QMRFlink"] != "null")
            //    {
            //        return Modelinfo["QMRFlink"];
            //    }
            //    return "https://www.vegahub.eu/vegahub-dwn/qmrf/qmrf-readme.txt";


            //}
            get { return System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("VegaAddins.dll", "guide/" + Modelinfo["GuideUrl"]); }
        }

        public TbQsarStatistics Statistics
        {
            get
            {
                return QsarAddinDefinitions.M4RatioModelStatistics;
            }
        }


        public TbQsarAddinFactory(Dictionary<string, string> Modelinfo)
        {
            this.Modelinfo = Modelinfo;

            //take info from excel (csv)
            Flags = TbObjectFlags.None;
            //Here should be exctracted from data
            ObjectId = new TbObjectId("VEGA - "+ Modelinfo["Modelname"], new Guid(Modelinfo["Guid"]), new Version(1, 0));
            ObjectAbout = QsarAddinDefinitions.GetM4ObjectAbout(Modelinfo);
            EndpointLocation = new List<string>()
    {
      Modelinfo["Endpoint location1"],
      Modelinfo["Endpoint location2"]
    };
            //deal with not reported duration key
            //TODO understand if duration can be a range
            Tuple<string, TbData> durationKeyValuePair = new Tuple<string, TbData>("Duration", new TbData(new TbUnit(TbScale.EmptyRatioScale.Name, string.Empty), new double?()));
            if (Modelinfo["Duration(unit)"] != "")
            {
                durationKeyValuePair = new Tuple<string, TbData>("Duration", new TbData(new TbUnit(TbScale.Time.Name, Modelinfo["Duration(unit)"]),
                   double.Parse(Modelinfo["Duration(value)"])));
            }

            Metadata = new TbMetadata((IReadOnlyDictionary<string, string>)QsarAddinDefinitions.getMetaDataValues(Modelinfo), (IReadOnlyDictionary<string, TbData>)new Dictionary<string, TbData>()
      {
        {
          durationKeyValuePair.Item1,
          durationKeyValuePair.Item2
        }
      });
            this.ScaleDeclaration = returnscale(Modelinfo);

        }

            public bool InitFactory(IList<string> errorLog, ITbInitTask initTask)
        {
            ////TODO add all units

            //allScales = initTask.ObjectCatalog.GetAllScales();





            return true;
        }
        public IReadOnlyList<ChemicalWithData> TrainingSet(ITbWorkTask task)
        {
            return (IReadOnlyList<ChemicalWithData>)null;
        }

        public IReadOnlyList<ChemicalWithData> GetTestSet(ITbWorkTask task)
        {
            return (IReadOnlyList<ChemicalWithData>)null;
        }

        public ITbQsar GetQsar(ITbWorkTask task)
        {
            return (ITbQsar)new QsarAddin(this.Modelinfo, this.ScaleDeclaration, this.ObjectId);
        }


        //methods to return the scale
        //TODO check if scale is returned correctly, add some throwing errors
        public TbScale returnscale(Dictionary<string, string> Modelinfo)
        {
            if (Modelinfo["Unit"] == "a-dimensional")
            {
            return (TbScale)new TbRatioScale(TbScale.MolarConcentration, "mol/L");
            }
            //generalize with unit family
            if (Modelinfo["Endpoint"] == "BCF")
            {
                return (TbScale)new TbRatioScale(TbScale.SpecificVolume, Modelinfo["Unit"]);
            }
            if (Modelinfo["UnitFamily"] == "Mass concentration")
            {
                return (TbScale)new TbRatioScale(TbScale.MassConcentration, Modelinfo["Unit"]);

            }
            if (Modelinfo["UnitFamily"] == "Molar concentration")
            {
                return (TbScale)new TbRatioScale(TbScale.MolarConcentration, Modelinfo["Unit"]);

            }
            if (Modelinfo["UnitFamily"] == "Time")
            {
                return (TbScale)new TbRatioScale(TbScale.Time, Modelinfo["Unit"]);

            }

            if (Modelinfo["Classes"] != "?")
                {
                    string[] classes = Modelinfo["Classes"].Split(new[] { ";" }, StringSplitOptions.None); ;
              return   new TbOrdinalScale(Modelinfo["tag"], Modelinfo["UnitFamily"], Guid.Parse(Modelinfo["ClassesGUID"]), classes);
            }

            if (Modelinfo["Unit"] == "Unknown"| Modelinfo["Unit"] == "no Unit")
            {

                return TbRatioScale.EmptyRatioScale;
            }
            //TODO add the correct tag
            return new TbRatioScale(Modelinfo["tag"], Modelinfo["UnitFamily"], Guid.Parse(Modelinfo["ClassesGUID"]), Modelinfo["Unit"]);
        }
    }
}
