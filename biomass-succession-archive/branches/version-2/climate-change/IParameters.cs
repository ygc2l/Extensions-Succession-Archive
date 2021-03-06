using Edu.Wisc.Forest.Flel.Util;
using System.Collections.Generic;

namespace Landis.Biomass.Succession.ClimateChange
{
    /// <summary>
    /// The biomass parameters affected by climate change.
    /// </summary>
    public interface IParameters
    {
        /// <summary>
        /// The maximum relative biomass for each shade class.
        /// </summary>
        Ecoregions.AuxParm<Percentage>[] MinRelativeBiomass
        {
            get;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Leaf longevity for each species.
        /// </summary>
        Species.AuxParm<double> LeafLongevity
        {
            get;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Bole decay rate for each species.
        /// </summary>
        Species.AuxParm<double> WoodyDecayRate
        {
            get;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Shape parameter for the mortality curve for each species.
        /// </summary>
        Species.AuxParm<double> MortCurveShapeParm
        {
            get;
        }

        Species.AuxParm<double> GrowthCurveShapeParm
        {
            get;
        }
        Species.AuxParm<double> LeafLignin {get;}
        //Species.AuxParm<double> growthR {get;}
        Ecoregions.AuxParm<int> AET {get;}

        //---------------------------------------------------------------------

        /// <summary>
        /// Definitions of sufficient light probabilities.
        /// </summary>
        List<ISufficientLight> LightClassProbabilities
        {
            get;
        }


        //---------------------------------------------------------------------

        /// <summary>
        /// Species' establishment probabilities for all ecoregions.
        /// </summary>
        Species.AuxParm<Ecoregions.AuxParm<double>> EstablishProbability
        {
            get;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Species' maximum growth rates for all ecoregions.
        /// </summary>
        Species.AuxParm<Ecoregions.AuxParm<int>> MaxANPP
        {
            get;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Species' maximum aboveground live biomass for all ecoregions.
        /// </summary>
        Species.AuxParm<Ecoregions.AuxParm<int>> MaxBiomass
        {
            get;
        }
    }
}
