using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private string _qmrflocation;
        private readonly TbUnit _calcUnit;


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


        public string QmrfLocation  {   get => this._qmrflocation;   private set => _qmrflocation = value;  }

        public TbQsarStatistics Statistics
        {
            get
            {
                return QsarAddinDefinitions.M4RatioModelStatistics(this.Modelinfo["tag"]);
            }
        }


        public TbQsarAddinFactory(Dictionary<string, string> Modelinfo)
        {
            this.Modelinfo = Modelinfo;

            //take info from excel (csv)
            Flags = TbObjectFlags.None;
            //Here should be exctracted from data
            ObjectId = new TbObjectId("VEGA - " + Modelinfo["Modelname"], new Guid(Modelinfo["Guid"]), new Version(1, 0));
            ObjectAbout = QsarAddinDefinitions.GetM4ObjectAbout(Modelinfo);
            List<string> EndpointL = new List<string>()
    {
      Modelinfo["Endpoint location1"],
      Modelinfo["Endpoint location2"]
    };
            if (Modelinfo["Endpoint location3"] != "")
            {
                EndpointL.Add(Modelinfo["Endpoint location3"]);
            }
            EndpointLocation = EndpointL;
            //deal with not reported duration key
            //TODO understand if duration can be a range
            Metadata = new TbMetadata((IReadOnlyDictionary<string, string>)QsarAddinDefinitions.getMetaDataValues(Modelinfo), null);
            if (Modelinfo["Duration(unit)"] != "")
            {
                Tuple<string, TbData> durationKeyValuePair = new Tuple<string, TbData>("Duration", new TbData(new TbUnit(TbScale.Time.Name, Modelinfo["Duration(unit)"]),
                   double.Parse(Modelinfo["Duration(value)"])));
                Metadata = new TbMetadata((IReadOnlyDictionary<string, string>)QsarAddinDefinitions.getMetaDataValues(Modelinfo), (IReadOnlyDictionary<string, TbData>)new Dictionary<string, TbData>()
      {
        {
          durationKeyValuePair.Item1,
          durationKeyValuePair.Item2
        }
      });
            }


            this.ScaleDeclaration = returnscale(Modelinfo);
            _calcUnit = new TbUnit(ScaleDeclaration.Name, Modelinfo["UnitName"]);

        }

        public bool InitFactory(IList<string> errorLog, ITbInitTask initTask)
        {

                    this._qmrflocation = "https://www.vegahub.eu/vegahub-dwn/qmrf/" + Modelinfo["QMRFlink"];

            
            ////TODO add all units

            //allScales = initTask.ObjectCatalog.GetAllScales();





            return true;
        }
        public IReadOnlyList<ChemicalWithData> TrainingSet(ITbWorkTask task)
        {
            return QsarAddinDefinitions.GetSet(this.Modelinfo, this.ScaleDeclaration, "Training");

        }

        public IReadOnlyList<ChemicalWithData> GetTestSet(ITbWorkTask task)
        {
            return QsarAddinDefinitions.GetSet(this.Modelinfo, this.ScaleDeclaration, "Test");
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
            if (Modelinfo["UnitFamily"] == "Unknown"| Modelinfo["UnitFamily"] == "Partition Coefficient")
            {
                return (TbScale)new TbRatioScale(TbScale.EmptyRatioScale, TbScale.EmptyRatioScale.BaseUnit);
            }
            if (Modelinfo["UnitFamily"] == "Specific volume")
            {
                return (TbScale)new TbRatioScale(TbScale.SpecificVolume, TbScale.SpecificVolume.BaseUnit);
            }
            if (Modelinfo["UnitFamily"] == "Mass concentration")
            {
                return (TbScale)new TbRatioScale(TbScale.MassConcentration, Modelinfo["UnitName"]);

            }
            if (Modelinfo["UnitFamily"] == "Molar concentration")
            {
                return (TbScale)new TbRatioScale(TbScale.MolarConcentration, Modelinfo["Unit"]);

            }
            //doesn't work. Ask help to LMC
            if (Modelinfo["UnitFamily"] == "Mass fraction")
            {
                //throw new WarningException(TbScale.MassFraction.FamilyGroup + TbScale.MassFraction.Identity + TbScale.MassFraction.Name + "\n" +
                //    TbScale.ConcentrationInBody_mass.Name + TbScale.ConcentrationInBody_mass.Identity + TbScale.ConcentrationInBody_mass.Name);
                return (TbScale)new TbRatioScale(Modelinfo["Endpoint"], "Mass fraction", Guid.Parse(Modelinfo["ClassesGUID"]), Modelinfo["UnitName"]);

            }
            if (Modelinfo["UnitFamily"] == "Pressure per mole")
            {
                return (TbScale)new TbRatioScale(TbScale.PressurePerMole, TbScale.PressurePerMole.BaseUnit);

            }
            //Time Doesn't support Unit conversion TODO ask LMC how to add Unit conversion, for now convert in prediction
            if (Modelinfo["UnitFamily"] == "Time")
            {
                return (TbScale)new TbRatioScale(TbScale.Time, TbScale.Time.BaseUnit);

            }
            if (Modelinfo["UnitFamily"] == "Administered dose(amount of substance)")
            {
                return (TbScale)new TbRatioScale(TbScale.AdministeredDose_mass, "mg/kg/day");

            }
            
            //try to uniform skin permeation units
            if (Modelinfo["UnitFamily"] == "Dose Rate (Area)")
            {
                return new TbRatioScale(Modelinfo["UnitFamily"], Modelinfo["UnitFamily"], Guid.Parse(Modelinfo["ClassesGUID"]), Modelinfo["Unit"]);

            }


            if (Modelinfo["Classes"] != "?")
            {
                //added a replacer to eliminate "
                string[] classes = Modelinfo["Classes"].Replace("\"", "").Split(new[] { ";" }, StringSplitOptions.None); ;
                return new TbOrdinalScale(Modelinfo["tag"], Modelinfo["UnitFamily"], Guid.Parse(Modelinfo["ClassesGUID"]), classes);
            }

            if (Modelinfo["Unit"] == "Unknown" | Modelinfo["Unit"] == "no Unit")
            {

                return TbRatioScale.EmptyRatioScale;
            }
            //TODO add the correct tag
            //return new TbRatioScale(Modelinfo["UnitFamily"], Modelinfo["UnitFamily"], Guid.Parse(Modelinfo["ClassesGUID"]), Modelinfo["Unit"]);
            return TbRatioScale.EmptyRatioScale;
        }
    }
}
