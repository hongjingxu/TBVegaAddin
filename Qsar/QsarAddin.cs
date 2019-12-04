using insilico.vega.vegadockcli;
using net.sf.jni4net;
using System;
using System.Collections.Generic;
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
            this.qsarUnit = new TbUnit(ScaleDeclaration.Name, Modelinfo["Unit"]);

        }

        public void Dispose()
        {
        }


        public TbScalarData Calculate(ITbBasket target)
        {

            Dictionary<string, string> ModelPred = this.RetrieveModelPreD(target, Modelinfo);

            string stringvalue = "error";
            if (ModelPred.ContainsKey("error"))
                throw new Exception(ModelPred["error"]);
            if (ModelPred.ContainsKey("prediction"))
                stringvalue = ModelPred["prediction"];

            ////check if the value is not predicted

            if (string.Equals(stringvalue, "-999.00") | string.Equals(stringvalue, "error", StringComparison.OrdinalIgnoreCase))
                //TODO add explanation to the error
                throw new Exception("The model is unable to give any prediction");



            //understand how to pass qualitative predictions
            if (this.ScaleDeclaration is TbQualitativeScale scaleDeclaration)
                {
                    //   TbQualitativeScale scaleD = (TbQualitativeScale)this.ScaleDeclaration;
                    if (!scaleDeclaration.Labels.Any<string>((Func<string, bool>)(l => l.Equals(stringvalue, StringComparison.InvariantCultureIgnoreCase))))
                    {
                        if (ModelPred["experimental"] != "-")
                        {
                            throw new Exception("Compound is not predicted, however has been provided the experimental value");
                        }
                        throw new Exception(stringvalue);
                    }
                    return (TbData)new TbData(new TbUnit(ScaleDeclaration.Name, stringvalue), new double?());
                }

                if (this.Modelinfo["Unit"] == "a-dimensional")
                {
                    double lambda = double.Parse(this.Modelinfo["Lambda"]);

                    double value = double.Parse(stringvalue, CultureInfo.InvariantCulture);

                    return (TbData)new TbData(new TbUnit(ScaleDeclaration.Name, "mol/L"), BoxCox(lambda, value));

                    //AFTER INTANTIATING ALL CLASSIFICATION MODELS RUN THIS   
                    //}
                    //if (this.Modelinfo["Unit"] == "no unit")
                    //{
                    //    return (TbData)new TbData(qsarUnit, runmodel(target, this.Modelinfo["tag"], "prediction"));

                }
                else
                {
                    double value = double.Parse(stringvalue, CultureInfo.InvariantCulture);
                    return (TbData)new TbData(qsarUnit, value);
                }

        }

        public ITbPrediction Predict(ITbBasket target)
        {
            target.WorkTask.TbToken.ThrowIfCancellationRequested();

            //understand how to pass scalar predictions

            TbData predictedTbData = (TbData)Calculate(target);
            Dictionary<string, string> ModelPred = this.RetrieveModelPreD(target, Modelinfo);

 
            // var predictedTbData = new TbData(predictedScalarData.Unit, predictedScalarData.Value);
            //var targetLogKow =
            //    target.WorkTask.CalcService.CalculateParameter(_logKowDescriptor.Descriptor, null, target);
            //mock descriptor

            TbData Mockdescriptordata = new TbData(this.qsarUnit, new double?());
            //TODO pack additional metadata into an unique object and then predicton probably will be faster
            

            Dictionary<string, string> AdditionalMetadata = new Dictionary<string, string>()
        {
          {
            "Guide name",

           this.Modelinfo["GuideUrl"]
          } };
            if (ModelPred.ContainsKey("assessment"))
                AdditionalMetadata.Add("Assessment", ModelPred["assessment"]);
            if (ModelPred.ContainsKey("assessment_verbose"))
                AdditionalMetadata.Add("Brief Explanation", ModelPred["assessment_verbose"]);
            if (ModelPred.ContainsKey("Similar_molecules_smiles"))
                AdditionalMetadata.Add("Analogues' SMILES", ModelPred["Similar_molecules_smiles"]);

            Dictionary<TbObjectId, TbData> matrixdescriptorvalues = new Dictionary<TbObjectId, TbData>()
            {
              {
                this.objectId,
                Mockdescriptordata
              }
                  };
            //run method Retrieve ADI, to retrieve all adi indexes
            Dictionary<string, TbData> ADImetadata = this.RetrieveAdi(target,Modelinfo);
            TbMetadata metadata = new TbMetadata((IReadOnlyDictionary<string, string>)AdditionalMetadata, (IReadOnlyDictionary<string, TbData>)ADImetadata);

            //return new PredictionAddin(predictedTbData, predictionDescription, xData);
            return new PredictionAddin(predictedTbData, metadata, matrixdescriptorvalues);
        }


        public bool IsRelevantToChemical(ITbBasket target, out string reason)
        {
            if (Regex.IsMatch(target.Chemical.Smiles, @"\."))
            {
                reason = "The model is not applicable because the compound is a disconnected structure";
                return false;
            }
            ////evaluate fif keep model running also for relevancy
            //Dictionary<string, string> ModelPred = RetrieveModelPreD(target, Modelinfo);
            //if (ModelPred.ContainsKey("error"))
            //{
            //    reason = ModelPred["error"];
            //    return false;
            //}
            reason = (string)null;
            return true;
        }


        public TbDomainStatus CheckDomain(ITbBasket target)
        {
            Dictionary<string, string> ModelPred = this.RetrieveModelPreD(target, Modelinfo);

            if (ModelPred.ContainsKey("error"))
            {
                return TbDomainStatus.Undefined;
            }
            double ADI;
            try
            {
                ADI = double.Parse(ModelPred["ADI"]);
            }
            catch
            {
                return TbDomainStatus.Undefined;
            }
            return ADI > 0.7 ? TbDomainStatus.InDomain : TbDomainStatus.OutOfDomain;
        }

        public string runmodel(ITbBasket target,  string output, Dictionary<string, string> Modelinfo)
        {
            string tag = this.Modelinfo["tag"];
            var setup = new BridgeSetup(false);
            //setup.AddAllJarsClassPath(@"B:\ToolboxAddinExamples\lib");

            setup.AddAllJarsClassPath(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            Bridge.CreateJVM(setup);
            Bridge.RegisterAssembly(typeof(VegaDockInterface).Assembly);
            java.lang.String stdout = VegaDockInterface.@getValues(tag, output, target.Chemical.Smiles);
           return stdout;
        }
        public Dictionary<string, string> RetrieveModelPreD(ITbBasket target, Dictionary<string, string> Modelinfo)
        {
            Dictionary<string, string> ModelPred = new Dictionary<string, string>();
            string tag = this.Modelinfo["tag"];
            var setup = new BridgeSetup(false);
            //setup.AddAllJarsClassPath(@"B:\ToolboxAddinExamples\lib");

            setup.AddAllJarsClassPath(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            Bridge.CreateJVM(setup);
            Bridge.RegisterAssembly(typeof(VegaDockObject).Assembly);
            VegaDockObject vdo = new VegaDockObject();
            vdo.run(tag,target.Chemical.Smiles);
            //If modelfor some reason gives any error, just return the error
            if (vdo.error.length() != 0)
            {
                ModelPred.Add("error", vdo.error);
                return ModelPred;
            }
            ModelPred.Add("prediction", vdo.prediction);
            ModelPred.Add("assessment", vdo.assessment);
            ModelPred.Add("assessment_verbose", vdo.assessment_verbose);
            ModelPred.Add("experimental", vdo.Experimental);
            ModelPred.Add("ADI", vdo.ADI);
            ModelPred.Add("Similar_molecules_index", vdo.Similar_molecules_index);
            ModelPred.Add("Similar_molecules_smiles", vdo.Similar_molecules_smiles);


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
            if (list.size()==0)
            {
                Adidictionary.Add("ADI Component", new TbData(new TbUnit(TbScale.EmptyRatioScale.Name, string.Empty), 0));
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
            return Math.Pow( value * lambda + 1.0, 1.0 / lambda);
        }


    }

}
