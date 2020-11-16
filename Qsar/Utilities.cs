using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Toolbox.Docking.Api.Data;
using Toolbox.Docking.Api.Units;

namespace VegaAddins.Qsar
{
    class Utilities
    {

        public static double BoxCox(double lambda, double value)
        {
            return Math.Pow(value * lambda + 1.0, 1.0 / lambda);
        }
        public static double DoubleParser(string value)
        {
            CultureInfo culture = new CultureInfo("en-US");
            //return double.Parse(value, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo);
            return double.Parse(value, culture.NumberFormat);
            //return double.Parse(value);
        }

        public static TbScalarData ConvertData(string stringvalue, TbScale ScaleDeclaration, Dictionary<string, string> Modelinfo)
        {


            //understand how to pass qualitative predictions
            if (ScaleDeclaration is TbQualitativeScale scaleDeclaration)
            {
                //   TbQualitativeScale scaleD = (TbQualitativeScale)this.ScaleDeclaration;
                if (!scaleDeclaration.Labels.Any<string>((Func<string, bool>)(l => l.Equals(stringvalue, StringComparison.InvariantCultureIgnoreCase))))
                    throw new Exception(string.Format("\"{0}\" is not a prediction for the declared scale.", (object)stringvalue));

                return (TbData)new TbData(new TbUnit(ScaleDeclaration.Name, stringvalue), new double?());
            }

            if (Modelinfo["Unit"] == "a-dimensional")
            {
                //labda is read from csv, for this reason should follow different rules than other parsers
                double lambda = DoubleParser(Modelinfo["Lambda"]);

                double value = DoubleParser(stringvalue);

                return (TbData)new TbData(new TbUnit(ScaleDeclaration.Name, "mmol/L"), BoxCox(lambda, value));

                //AFTER INTANTIATING ALL CLASSIFICATION MODELS RUN THIS   
                //}
                //if (this.Modelinfo["Unit"] == "no unit")
                //{
                //    return (TbData)new TbData(qsarUnit, runmodel(target, this.Modelinfo["tag"], "prediction"));

            }
            //workaroud for the lack of conversion for Log unitfamily
            Regex regexloginv = new Regex(@"log\(1/.*");
            //Doesn't work, don't ask why
            if (regexloginv.IsMatch(Modelinfo["Unit"]))
            {
                double value = DoubleParser(stringvalue);

                return (TbData)new TbData(new TbUnit(ScaleDeclaration.Name, Modelinfo["UnitName"]), Math.Pow(10, value * -1));
            }

            Regex regex = new Regex(@"log\(.*");
            //Doesn't work, don't ask why
            if (regex.IsMatch(Modelinfo["Unit"])& Modelinfo["Unit"]!="log(cm/h)")
            {
                double value = DoubleParser(stringvalue);


                return (TbData)new TbData(new TbUnit(ScaleDeclaration.Name, Modelinfo["UnitName"]), Math.Pow(10, value));
            }
            else
            {

                double value = DoubleParser(stringvalue);
                return (TbData)new TbData(new TbUnit(ScaleDeclaration.Name, Modelinfo["UnitName"]), value);
            }
        }


    }
}
