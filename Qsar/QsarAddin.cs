using insilico.vega.vegadockcli;
using net.sf.jni4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
           
            
            string stringvalue = runmodel(target, "prediction", Modelinfo);

            //check if the value is not predicted

            if (stringvalue == "-999.00")
                //TODO add explanation to the error
                throw new Exception("The model is unable to give any prediction");
              


            //understand how to pass qualitative predictions
            if ( this.ScaleDeclaration is TbQualitativeScale scaleDeclaration)
            {    
             //   TbQualitativeScale scaleD = (TbQualitativeScale)this.ScaleDeclaration;
            if (!scaleDeclaration.Labels.Any<string>((Func<string, bool>)(l => l.Equals(stringvalue, StringComparison.InvariantCultureIgnoreCase))))
                throw new Exception(string.Format("\"{0}\" is not a prediction for the declared scale.", (object)stringvalue));

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
            // var predictedTbData = new TbData(predictedScalarData.Unit, predictedScalarData.Value);
            //var targetLogKow =
            //    target.WorkTask.CalcService.CalculateParameter(_logKowDescriptor.Descriptor, null, target);
            //mock descriptor



            TbData Mockdescriptordata = new TbData(this.qsarUnit, new double?());
            Dictionary<string, string> AdditionalMetadata = new Dictionary<string, string>()
        {
          {
            "guide",

           this.Modelinfo["GuideUrl"]
          },
                                          {
                    "Assessment",
                 this.runmodel(target, "assessment", Modelinfo)
        },

                {
                    "Brief Explanation",
                 this.runmodel(target, "assessment_verbose", Modelinfo)
        },

                                                {
                    "Analogues' SMILES",
                 this.runmodel(target, "Similar_molecules_Smiles", Modelinfo).Replace(";","\n")
        }
                ,
                                {
                    "Message Error",
                 this.runmodel(target, "error", Modelinfo)
        }

        };

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
            string stringvalue = runmodel(target, "prediction", Modelinfo);
            if (stringvalue == "-999.00")
            {
                reason = "It's not possible to perform the prediction";
                return false;
            }
            reason = (string)null;
            return true;
        }


        public TbDomainStatus CheckDomain(ITbBasket target)
        {
            
            string DomainIndex = this.runmodel(target, "adi", Modelinfo);
            if (DomainIndex== null)
            {
                return TbDomainStatus.Undefined;
            }
            return double.Parse(DomainIndex) > 0.5 ? TbDomainStatus.InDomain : TbDomainStatus.OutOfDomain;
        }

        public string runmodel(ITbBasket target,  string output, Dictionary<string, string> Modelinfo)
        {
            string tag = this.Modelinfo["tag"];
            var setup = new BridgeSetup(false);
            //setup.AddAllJarsClassPath(@"B:\ToolboxAddinExamples\lib");

            setup.AddAllJarsClassPath(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            Bridge.CreateJVM(setup);
            Bridge.RegisterAssembly(typeof(ModelsList).Assembly);
            Bridge.RegisterAssembly(typeof(VegaDockInterface).Assembly);
            java.lang.String stdout = VegaDockInterface.@getValues(tag, output, target.Chemical.Smiles);
           return stdout;
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
                Adidictionary.Add("ADI", new TbData(new TbUnit(TbScale.EmptyRatioScale.Name, string.Empty), new double?()));
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
