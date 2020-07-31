using insilico.vega.vegadockcli;
using net.sf.jni4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Toolbox.Docking.Api.Control;
using Toolbox.Docking.Api.Data;
using Toolbox.Docking.Api.Objects;
using Toolbox.Docking.Api.Objects.Calculator;
using Toolbox.Docking.Api.Objects.Qsar;
using Toolbox.Docking.Api.Units;

namespace VegaAddins.Qsar
{
    public class QsarAddin : ITbQsar, ITbCalculator, ITbObject, IDisposable, ITbObjectDomain
    {
        private readonly TbScale ScaleDeclaration;
        private readonly TbUnit qsarUnit;
        private readonly TbObjectId objectId;
        private readonly Dictionary<string, string> Modelinfo;


        public QsarAddin(Dictionary<string, string> Modelinfo, TbScale ScaleDeclaration, TbObjectId objectId)
        {
            this.Modelinfo = Modelinfo;
            this.ScaleDeclaration = ScaleDeclaration;
            this.objectId = objectId;
            //CHANGED TO UNITNAME, not anymore log units
            this.qsarUnit = new TbUnit(ScaleDeclaration.Name, Modelinfo["UnitName"]);

        }

        public void Dispose()
        {
        }

        public TbScalarData Calculate(ITbBasket target)
        {
            Dictionary<string, string> ModelPred = this.RetrieveModelPreD(target, Modelinfo);

            if (ModelPred.ContainsKey("error"))
                throw new Exception(ModelPred["error"]);

            

           return Utilities.ConvertData(ModelPred["prediction"], this.ScaleDeclaration, Modelinfo);
        }
        public ITbPrediction Predict(ITbBasket target)
        {
            target.WorkTask.TbToken.ThrowIfCancellationRequested();

            //understand how to pass scalar predictions
            TbData predictedTbData = (TbData)Calculate(target);
            // var predictedTbData = new TbData(predictedScalarData.Unit, predictedScalarData.Value);
            //var targetLogKow =
            //    target.WorkTask.CalcService.CalculateParameter(_logKowDescriptor.Descriptor, null, target);
            //mock descriptor
            Dictionary<string, string> ModelPred = this.RetrieveModelPreD(target, Modelinfo);

            TbData Mockdescriptordata = new TbData(new TbUnit(TbScale.EmptyRatioScale.FamilyGroup, TbScale.EmptyRatioScale.BaseUnit), new double?());
            //TODO pack additional metadata into an unique object and then predicton probably will be faster

            Dictionary<string, string> AdditionalMetadata = new Dictionary<string, string>()
        {
          {
            "Guide name",

           this.Modelinfo["GuideUrl"]
          }  };

            if (ModelPred.ContainsKey("assessment"))
                AdditionalMetadata.Add("Assessment", ModelPred["assessment"]);
            if (ModelPred.ContainsKey("assessment_verbose"))
                AdditionalMetadata.Add("Brief Explanation", ModelPred["assessment_verbose"]);
            if (ModelPred.ContainsKey("Similar_molecules_smiles"))
                AdditionalMetadata.Add("Analogues' SMILES", ModelPred["Similar_molecules_smiles"]);
            //if (Modelinfo.ContainsKey("QMRFlink"))
            //    AdditionalMetadata.Add("QMRF", Modelinfo["QMRFlink"]);
            //if (Modelinfo.ContainsKey("Lambda"))
            //{
            // AdditionalMetadata.Add("Lambda", double.Parse(this.Modelinfo["Lambda"], CultureInfo.InvariantCulture).ToString());

            //}
            Dictionary<TbObjectId, TbData> matrixdescriptorvalues = new Dictionary<TbObjectId, TbData>()
            {
              {
                this.objectId,
                Mockdescriptordata
              }
                  };
            //run method Retrieve ADI, to retrieve all adi indexes
            Dictionary<string, TbData> ADImetadata = this.RetrieveAdi(target, Modelinfo);
            TbMetadata metadata = new TbMetadata((IReadOnlyDictionary<string, string>)AdditionalMetadata, (IReadOnlyDictionary<string, TbData>)ADImetadata);

            //return new PredictionAddin(predictedTbData, predictionDescription, xData);
            return new PredictionAddin(predictedTbData, metadata, matrixdescriptorvalues);
        }


        public bool IsRelevantToChemical(ITbBasket target, out string reason)
        {

            Dictionary<string, string> ModelPred = RetrieveModelPreD(target, Modelinfo);
            if (ModelPred.ContainsKey("error"))
            {
                reason = ModelPred["error"];
                return false;
            }
            reason = (string)null;
            return true;
        }
        public bool IsDisconnected(ITbBasket target)
        {
            string smiles = target.Chemical.Smiles;
            Regex regex = new Regex(@"\.");
            //Doesn't work, don't ask why
            if (regex.IsMatch(smiles))
            {
                return true;
            }
            return false;

        }


        public TbDomainStatus CheckDomain(ITbBasket target)
        {
            Dictionary<string, string> ModelPred = RetrieveModelPreD(target, Modelinfo);

            if (ModelPred.ContainsKey("error"))
            {
                return TbDomainStatus.Undefined;
            }
            //double ADI;
            //try
            //{
            //    ADI = DoubleParser(ModelPred["ADI"]);
            //}
            //catch
            //{
            //    return TbDomainStatus.Undefined;
            //}
            //return ADI > 0.7 ? TbDomainStatus.InDomain : TbDomainStatus.OutOfDomain;
            Regex regex = new Regex(@".*low reliability.*");
           return regex.IsMatch(ModelPred["assessment_verbose"]) ? TbDomainStatus.OutOfDomain : TbDomainStatus.InDomain;
        }

        //public string runmodel(ITbBasket target, string output, Dictionary<string, string> Modelinfo)
        //{
        //    string tag = this.Modelinfo["tag"];
        //    var setup = new BridgeSetup(false);
        //    //setup.AddAllJarsClassPath(@"B:\ToolboxAddinExamples\lib");

        //    setup.AddAllJarsClassPath(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
        //    Bridge.CreateJVM(setup);
        //    Bridge.RegisterAssembly(typeof(VegaDockInterface).Assembly);
        //    java.lang.String stdout = VegaDockInterface.@getValues(tag, output, target.Chemical.Smiles);
        //    return stdout;
        //}
        public Dictionary<string, string> RetrieveModelPreD(ITbBasket target, Dictionary<string, string> Modelinfo)
        {
            Dictionary<string, string> ModelPred = new Dictionary<string, string>();
            if (IsDisconnected(target))
            {
                ModelPred.Add("error", "Unable to run the model. SMILES provided is a Disconnected Structure");
                return ModelPred;
            }

            string tag = this.Modelinfo["tag"];
            var setup = new BridgeSetup(false);
            //setup.AddAllJarsClassPath(@"B:\ToolboxAddinExamples\lib");

            setup.AddAllJarsClassPath(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            Bridge.CreateJVM(setup);
            Bridge.RegisterAssembly(typeof(VegaDockObject).Assembly);
            VegaDockObject vdo = new VegaDockObject();
            vdo.run(tag, target.Chemical.Smiles);

            if (VegaDockObject.error.length() != 0)
            {
                ModelPred.Add("error", VegaDockObject.error);
                return ModelPred;
            }
            ModelPred.Add("prediction", VegaDockObject.prediction);
            ModelPred.Add("assessment", VegaDockObject.assessment);
            ModelPred.Add("assessment_verbose", VegaDockObject.assessment_verbose);
            ModelPred.Add("Experimental", VegaDockObject.Experimental);
            ModelPred.Add("ADI", VegaDockObject.ADI);
            ModelPred.Add("Similar_molecules_index", VegaDockObject.Similar_molecules_index);
            ModelPred.Add("Similar_molecules_smiles", VegaDockObject.Similar_molecules_smiles);


            return ModelPred;
        }
        public Dictionary<string, TbData> RetrieveAdi(ITbBasket target, Dictionary<string, string> Modelinfo)
        {
            Dictionary<string, TbData> Adidictionary = new Dictionary<string, TbData>();
            string tag = this.Modelinfo["tag"];
            var setup = new BridgeSetup(false);
            //setup.AddAllJarsClassPath(@"B:\ToolboxAddinExamples\lib");

            setup.AddAllJarsClassPath(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            Bridge.CreateJVM(setup);

            Bridge.RegisterAssembly(typeof(AdiArray).Assembly);
            java.util.List list = AdiArray.getADI(tag, target.Chemical.Smiles);
            if (list.size() == 0)
            {

                return Adidictionary;
            }

            //create the iterator
            for (int i = 0; i < list.size(); i++)
            {

                AdiArray adicomponent = (AdiArray)list.get(i);
                Adidictionary.Add(adicomponent.AdiName, new TbData(new TbUnit(TbScale.EmptyRatioScale.Name, string.Empty), adicomponent.AdiValue));
            }

            return Adidictionary;
        }


        public double BoxCox(double lambda, double value)
        {
            return Math.Pow(value * lambda + 1.0, 1.0 / lambda);
        }
        public double DoubleParser( string value)
        {
            CultureInfo culture = new CultureInfo("en-US");
            //return double.Parse(value, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo);
            return double.Parse(value, culture.NumberFormat);
            //return double.Parse(value);
        }


    }

}
