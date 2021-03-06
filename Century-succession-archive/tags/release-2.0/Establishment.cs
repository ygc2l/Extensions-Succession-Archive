//  Copyright 2007-2010 Portland State University, University of Wisconsin-Madison
//  Author: Robert Scheller, Ben Sulman

using Landis.Core;
using Landis.SpatialModeling;
using Edu.Wisc.Forest.Flel.Util;
using Landis.Library.Succession;

using System;
using System.IO;
using Landis.Library.Climate;

namespace Landis.Extension.Succession.Century
{
    public class Establishment
    {

        private static StreamWriter log;


        public static void Initialize()
        {
            string logFileName   = "Century-prob-establish-log.csv"; 
            PlugIn.ModelCore.Log.WriteLine("   Opening a Century log file \"{0}\" ...", logFileName);
            try {
                log = PlugIn.ModelCore.CreateTextFile(logFileName);
            }
            catch (Exception err) {
                string mesg = string.Format("{0}", err.Message);
                throw new System.ApplicationException(mesg);
            }
            
            log.AutoFlush = true;
            log.WriteLine("Time, Ecoregion, Species, TempMult, MinJanTempMult, SoilMoistureMult, ProbEst");
        }


        //---------------------------------------------------------------------
        // Note:  If the succession time step is > 0, then an average Pest is calculated,
        // based on each ecoregion-climate year.  The average is used because establishment 
        // determines reproductive success, which occurs only once for the given successional
        // time step.
        // Note:  If one of the three multipliers dominates, 
        // expect to see Pest highly correlated with that multiplier.
        // Note:  N is not included here as it is site level quality and Pest represent
        // ecoregion x climate qualities.  N limits have been folded into PlugIn.SufficientLight.
        public static Species.AuxParm<Ecoregions.AuxParm<double>>  GenerateNewEstablishProbabilities(int years)
        {
            Species.AuxParm<Ecoregions.AuxParm<double>> EstablishProbability;
            EstablishProbability = CreateSpeciesEcoregionParm<double>(PlugIn.ModelCore.Species, PlugIn.ModelCore.Ecoregions);

            double[,] avgTempMultiplier = new double[PlugIn.ModelCore.Species.Count, PlugIn.ModelCore.Ecoregions.Count];
            double[,] avgSoilMultiplier = new double[PlugIn.ModelCore.Species.Count, PlugIn.ModelCore.Ecoregions.Count];
            double[,] avgMinJanTempMultiplier = new double[PlugIn.ModelCore.Species.Count, PlugIn.ModelCore.Ecoregions.Count];
            
            for (int y = 0; y < years; ++y) 
            {
                foreach(IEcoregion ecoregion in PlugIn.ModelCore.Ecoregions) 
                {
                    if(!ecoregion.Active || EcoregionData.ActiveSiteCount[ecoregion] < 1)
                        continue;

                    AnnualClimate ecoClimate = EcoregionData.AnnualClimateArray[ecoregion][y];
                
                    if(ecoClimate == null)
                        throw new System.ApplicationException("Error: CLIMATE NULL.");
                    
                
                    double ecoDryDays = CalculateSoilMoisture(ecoClimate, ecoregion, y);
                
                    foreach(ISpecies species in PlugIn.ModelCore.Species)
                    {
                        double tempMultiplier = BotkinDegreeDayMultiplier(ecoClimate, species);
                        double soilMultiplier = SoilMoistureMultiplier(ecoClimate, species, ecoDryDays);
                        double minJanTempMultiplier = MinJanuaryTempModifier(ecoClimate, species);
                    
                        // Liebig's Law of the Minimum is applied to the four multipliers for each year:
                        double minMultiplier = System.Math.Min(tempMultiplier, soilMultiplier);
                        minMultiplier = System.Math.Min(minJanTempMultiplier, minMultiplier);
                        
                        EstablishProbability[species][ecoregion] += minMultiplier;
                        
                        avgTempMultiplier[species.Index, ecoregion.Index] += tempMultiplier;
                        avgSoilMultiplier[species.Index, ecoregion.Index] += soilMultiplier;
                        avgMinJanTempMultiplier[species.Index, ecoregion.Index] += minJanTempMultiplier;
                    }
                }            
            }
            

            foreach(IEcoregion ecoregion in PlugIn.ModelCore.Ecoregions) 
            {
                foreach(ISpecies species in PlugIn.ModelCore.Species)
                {
                    EstablishProbability[species][ecoregion] /= (double) years;
                    
                    if(PlugIn.ModelCore.CurrentTime > 0 && EcoregionData.ActiveSiteCount[ecoregion] > 0)
                    {
                        avgTempMultiplier[species.Index, ecoregion.Index] /= (double) years;
                        avgSoilMultiplier[species.Index, ecoregion.Index] /= (double) years;
                        avgMinJanTempMultiplier[species.Index, ecoregion.Index] /= (double) years;

                        log.Write("{0}, {1}, {2},", PlugIn.ModelCore.CurrentTime, ecoregion.Name, species.Name);
                        log.Write("{0:0.00},", avgTempMultiplier[species.Index, ecoregion.Index]);
                        log.Write("{0:0.00},", avgMinJanTempMultiplier[species.Index, ecoregion.Index]);
                        log.Write("{0:0.00},", avgSoilMultiplier[species.Index, ecoregion.Index]);
                        log.WriteLine("{0:0.00}", EstablishProbability[species][ecoregion]);
                    }
                }
            }     
            
            //Reproduction.ChangeEstablishProbabilities(Util.ToArray<double>(EstablishProbability));

            
            return EstablishProbability;

        }
        
        //---------------------------------------------------------------------------
        private static double SoilMoistureMultiplier(AnnualClimate weather, ISpecies species, double dryDays)
        //Calc soil moisture multipliers based on Degree_Day (supplied by calc_temperature()),
        //dryDays (supplied by MOIST).

        {
            double sppAllowableDrought = SpeciesData.MaxDrought[species];
            double growDays = 0.0;
            double maxDrought;
            double Soil_Moist_GF = 0.0;

            growDays = weather.EndGrowing - weather.BeginGrowing + 1.0;
            if (growDays < 2.0)
            {
                PlugIn.ModelCore.Log.WriteLine("Begin Grow = {0}, End Grow = {1}", weather.BeginGrowing, weather.EndGrowing);
                throw new System.ApplicationException("Error: Too few growing days.");
            }
            //Calc species soil moisture multipliers
            maxDrought = sppAllowableDrought * growDays;
            
            //PlugIn.ModelCore.Log.WriteLine("SppMaxDr={0:0.00}, growDays={1:0.0}, dryDays={2:0.0}.", sppAllowableDrought, growDays, dryDays);
            if (maxDrought < dryDays) 
            {
                Soil_Moist_GF = 0.0;
            }
            else
            {
                Soil_Moist_GF = System.Math.Sqrt((double)(maxDrought - dryDays) / maxDrought);
            }
            return Soil_Moist_GF;
        }
        
        //---------------------------------------------------------------------------
        private static double BotkinDegreeDayMultiplier(AnnualClimate weather, ISpecies species)
        {

            //Calc species degree day multipliers  
            //Botkin et al. 1972. J. Ecol. 60:849 - 87
            
            double max_Grow_Deg_Days = SpeciesData.GDDmax[species]; 
            double min_Grow_Deg_Days = SpeciesData.GDDmin[species];
            
            double Deg_Day_GF = 0.0;
            double Deg_Days = (double) weather.GrowingDegreeDays; 
            double totalGDD = max_Grow_Deg_Days - min_Grow_Deg_Days;
            
            Deg_Day_GF = (4.0 * (Deg_Days - min_Grow_Deg_Days) * 
                  (max_Grow_Deg_Days - Deg_Days)) / (totalGDD * totalGDD);
            
           if (Deg_Day_GF < 0) Deg_Day_GF = 0.0;     
           
           return Deg_Day_GF;
        }
        
        //---------------------------------------------------------------------------
        private static double MinJanuaryTempModifier(AnnualClimate weather, ISpecies species)
        // Is the January mean temperature greater than the species specified minimum?
        {
        
            int speciesMinimum = SpeciesData.MinJanTemp[species];
            
            if (weather.MonthlyTemp[0] < speciesMinimum)
                return 0.0;
            else
                return 1.0;
        }
        
        //---------------------------------------------------------------------------
        private static double CalculateSoilMoisture(AnnualClimate weather, IEcoregion ecoregion, int year)
        // Calculate fraction of growing season with unfavorable soil moisture
        // for growth (Dry_Days_in_Grow_Seas) used in SoilMoistureMultiplier to determine soil
        // moisture growth multipliers.
        //
        // Simulates method of Thorthwaite and Mather (1957) as modified by Pastor and Post (1984).
        //
        // This method necessary to estimate annual soil moisture at the ECOREGION scale, whereas
        // the SiteVar AvailableWater exists at the site level and is updated monthly.
        
        //field_cap = centimeters of water the soil can hold at field capacity
        //field_dry = centimeters of water below which tree growth stops
        //            (-15 bars)
        // NOTE:  Because the original LINKAGES calculations were based on a 100 cm rooting depth, 
        // 100 cm are used here, although soil depth may be given differently for Century 
        // calculations.
        
        //beg_grow_seas = year day on which the growing season begins
        //end_grow_seas = year day on which the growing season ends
        //latitude = latitude of region (degrees north)

        {

            double  xFieldCap,            //
            waterAvail,           //
            aExponentET,        //
            oldWaterAvail,      //
            monthlyRain,         //
            potWaterLoss,       //
            potentialET,
            tempFac,             //
            xAccPotWaterLoss, //
            changeSoilMoisture, //
            oldJulianDay,       //
            dryDayInterp;       //
            //double fieldCapacity = EcoregionData.FieldCapacity[ecoregion] * (double) EcoregionData.SoilDepth[ecoregion];
            //double wiltingPoint  = EcoregionData.WiltingPoint[ecoregion] * (double) EcoregionData.SoilDepth[ecoregion];
            double fieldCapacity = EcoregionData.FieldCapacity[ecoregion] * 100.0;
            double wiltingPoint  = EcoregionData.WiltingPoint[ecoregion] * 100.0;
            
            //Initialize water content of soil in January to Field_Cap (mm)
            xFieldCap = 10.0 * fieldCapacity;
            waterAvail = fieldCapacity;


            //Initialize Thornwaithe parameters:
            //
            //TE = temperature efficiency
            //aExponentET = exponent of evapotranspiration function
            //pot_et = potential evapotranspiration
            //aet = actual evapotranspiration
            //acc_pot_water_loss = accumulated potential water loss
    
            double actualET = 0.0;
            double accPotWaterLoss = 0.0;
            double tempEfficiency = 0.0;
  
            for (int i = 0; i < 12; i++) 
            {
                tempFac = 0.2 * weather.MonthlyTemp[i];
      
                if (tempFac > 0.0)
                    tempEfficiency += System.Math.Pow(tempFac, 1.514);
            }
    
            aExponentET = 0.675 * System.Math.Pow(tempEfficiency, 3) - 
                            77.1 * (tempEfficiency * tempEfficiency) +
                            17920.0 * tempEfficiency + 492390.0;
            aExponentET *= (0.000001);
    
            //Initialize the number of dry days and current day of year
            int dryDays = 0;
            double julianDay = 15.0;
            double annualPotentialET = 0.0;
    
    
            for (int i = 0; i < 12; i++) 
            {
                double daysInMonth = AnnualClimate.DaysInMonth(i, year);
                oldWaterAvail = waterAvail;
                monthlyRain = weather.MonthlyPrecip[i];
                tempFac = 10.0 * weather.MonthlyTemp[i];
                
                //Calc potential evapotranspiriation (potentialET) Thornwaite and Mather,
                //1957.  Climatology 10:83 - 311.
                if (tempFac > 0.0) 
                {

                    potentialET = 1.6 * (System.Math.Pow((tempFac / tempEfficiency), aExponentET)) * 
                            AnnualClimate.LatitudeCorrection(i, EcoregionData.Latitude[ecoregion]);
                } 
                else 
                {
                    potentialET = 0.0;
                }
                
                annualPotentialET += potentialET;
      
                //Calc potential water loss this month
                potWaterLoss = monthlyRain - potentialET;
      
                //If monthlyRain doesn't satisfy potentialET, add this month's potential
                //water loss to accumulated water loss from soil
                if (potWaterLoss < 0.0) 
                {
                    accPotWaterLoss += potWaterLoss;
                    xAccPotWaterLoss = accPotWaterLoss * 10;
      
                    //Calc water retained in soil given so much accumulated potential
                    //water loss Pastor and Post. 1984.  Can. J. For. Res. 14:466:467.
      
                    waterAvail = fieldCapacity * 
                                 System.Math.Exp((.000461 - 1.10559 / xFieldCap) * (-1.0 * xAccPotWaterLoss));
      
                    if (waterAvail < 0.0)
                        waterAvail = 0.0;
      
                    //changeSoilMoisture - during this month
                    changeSoilMoisture = waterAvail - oldWaterAvail;
      
                    //Calc actual evapotranspiration (AET) if soil water is drawn down
                    actualET += (monthlyRain - changeSoilMoisture);
                } 

                //If monthlyRain satisfies potentialET, don't draw down soil water
                else 
                {
                    waterAvail = oldWaterAvail + potWaterLoss;
                    if (waterAvail >= fieldCapacity)
                        waterAvail = fieldCapacity;
                    changeSoilMoisture = waterAvail - oldWaterAvail;
 
                    //If soil partially recharged, reduce accumulated potential
                    //water loss accordingly
                    accPotWaterLoss += changeSoilMoisture;
      
                    //If soil completely recharged, reset accumulated potential
                    //water loss to zero
                    if (waterAvail >= fieldCapacity)
                        accPotWaterLoss = 0.0;
      
                    //If soil water is not drawn upon, add potentialET to AET
                    actualET += potentialET;
                }
      
                oldJulianDay = julianDay;
                julianDay += daysInMonth;
                dryDayInterp = 0.0;

                //Increment number of dry days, truncate
                //at end of growing season
                if ((julianDay > weather.BeginGrowing) && (oldJulianDay < weather.EndGrowing)) 
                {
                    if ((oldWaterAvail >= wiltingPoint)  && (waterAvail >= wiltingPoint))
                    {
                        dryDayInterp += 0.0;  // NONE below wilting point
                    }
                    else if ((oldWaterAvail > wiltingPoint) && (waterAvail < wiltingPoint)) 
                    {
                        dryDayInterp = daysInMonth * (wiltingPoint - waterAvail) / 
                                        (oldWaterAvail - waterAvail);
                        if ((oldJulianDay < weather.BeginGrowing) && (julianDay > weather.BeginGrowing))
                            if ((julianDay - weather.BeginGrowing) < dryDayInterp)
                                dryDayInterp = julianDay - weather.BeginGrowing;
    
                        if ((oldJulianDay < weather.EndGrowing) && (julianDay > weather.EndGrowing))
                            dryDayInterp = weather.EndGrowing - julianDay + dryDayInterp;
    
                        if (dryDayInterp < 0.0)
                            dryDayInterp = 0.0;
    
                    } 
                    else if ((oldWaterAvail < wiltingPoint) && (waterAvail > wiltingPoint)) 
                    {
                        dryDayInterp = daysInMonth * (wiltingPoint - oldWaterAvail) / 
                                        (waterAvail - oldWaterAvail);
          
                        if ((oldJulianDay < weather.BeginGrowing) && (julianDay > weather.BeginGrowing))
                            dryDayInterp = oldJulianDay + dryDayInterp - weather.BeginGrowing;
    
                        if (dryDayInterp < 0.0)
                            dryDayInterp = 0.0;
    
                        if ((oldJulianDay < weather.EndGrowing) && (julianDay > weather.EndGrowing))
                            if ((weather.EndGrowing - oldJulianDay) < dryDayInterp)
                                dryDayInterp = weather.EndGrowing - oldJulianDay;
                    } 
                    else // ALL below wilting point
                    {
                        dryDayInterp = daysInMonth;
          
                        if ((oldJulianDay < weather.BeginGrowing) && (julianDay > weather.BeginGrowing))
                            dryDayInterp = julianDay - weather.BeginGrowing;
    
                        if ((oldJulianDay < weather.EndGrowing) && (julianDay > weather.EndGrowing))
                            dryDayInterp = weather.EndGrowing - oldJulianDay;
                    }
      
                    dryDays += (int) dryDayInterp;
                }
            }  //END MONTHLY CALCULATIONS
  
            //Convert AET from cm to mm
            //actualET *= 10.0;

            //Calculate AET multiplier
            //(used to be done in decomp)
            //float aetMf = min((double)AET,600.0);
            //AET_Mult = (-1. * aetMf) / (-1200. + aetMf);
            
            return dryDays;
        }
        //---------------------------------------------------------------------

        private static Species.AuxParm<Ecoregions.AuxParm<double>> CreateSpeciesEcoregionParm<T>(ISpeciesDataset speciesDataset, IEcoregionDataset ecoregionDataset)
        {
            Species.AuxParm<Ecoregions.AuxParm<double>> newParm;
            newParm = new Species.AuxParm<Ecoregions.AuxParm<double>>(speciesDataset);
            foreach (ISpecies species in speciesDataset) {
                newParm[species] = new Ecoregions.AuxParm<double>(ecoregionDataset);
            }
            return newParm;
        }
        
        
    }
}
