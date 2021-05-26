﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Statistics;

namespace PTMStoichiometry20210414a
{
    //work with protein data from MM and FlashLFQ
    public class ProteinGroup
    {
        public List<Peptide> PeptidesInProtein { get; }
        public string ProteinName { get; }
        public string useProt { get; } //determine whether prot is useful for stochiometry calc - needs to have both mod & unmod state

        public int NumPeptidesInProtein { get; }
        public List<Peptide> BaselinePeptides { get; } //peptides to compare others to: must be unmodified, covary with each other, and not be in other proteins & must be the same baseline for all groups
        public List<PairwiseComparison> ProteinPairwiseComparisons { get; } //compare peptides by group within protein
        public ProteinGroup(string proteinAccession, List<Peptide> peptides, List<string> groups)
        {
            this.ProteinName = proteinAccession;
            this.PeptidesInProtein = peptides.Where(p => p.ProteinGroup == ProteinName).ToList(); //need to filter peptides - only want peptides which are in all groups and observed in 2/3 observations
            this.NumPeptidesInProtein = PeptidesInProtein.Count();
            this.useProt = isProteinUseful(PeptidesInProtein);
            if ((this.useProt == "modandunmod")) //only calc stoichiometries if there are multiple peptides both mod & unmod - TODO: consider expanding
            {
                this.BaselinePeptides = getBaseLinePeptides(this.PeptidesInProtein, peptides, groups, 0.25);

                if (this.BaselinePeptides.Count() > 2)
                {
                    this.ProteinPairwiseComparisons = calcComparison(groups);
                }
            }
        }

        //function to find baseline peptides to compare against
        private List<Peptide> getBaseLinePeptides(List<Peptide> peptidesInProtein, List<Peptide> Allpeptides, List<string> groups, double covarianceStrength)
        {
            List<Peptide>  unmodPep = peptidesInProtein.Where(p => p.Sequence == p.BaseSeq).Where(p => p.IsUnique).ToList();
            //List<Peptide> AllOtherPeptides = Allpeptides.Where(p => !unmodPep.Contains(p)).ToList();
            //foreach (Peptide pep in unmodPep)
            //{
            //    if (AllOtherPeptides.Contains(pep))
          //      {
          //          unmodPep.Remove(pep);
          //      }
          //  }

            //if pos Cov(X,Y) and pos Cov(X,Z) => pos Cov(Y,Z)
            List<Peptide> unmodPepCov = new List<Peptide>();
            for (int p1 = 0; p1 < unmodPep.Count(); p1++) //TODO: question - not considering groups right now - think this is okay bc all unmodified...?
            {
                List<Peptide> temp = new List<Peptide>();
                temp.Add(unmodPep[p1]);
                for (int p2 = p1+1; p2 < unmodPep.Count(); p2++)
                {

                    if (GroupCorrelation(unmodPep[p1].Intensities, unmodPep[p2].Intensities, groups) > covarianceStrength) //TODO: tune covarianceStrength
                        temp.Add(unmodPep[p2]);
                }

                if (temp.Count() > unmodPepCov.Count())
                {
                    unmodPepCov = temp;
                }
            }

            return unmodPepCov;
        }

        //function to calc Correlation in each group - returns min correlation
        private double GroupCorrelation(List<Intensity> Peptide1, List<Intensity> Peptide2, List<string> groups)
        {
            List<Intensity> Peptide1Vals = Peptide1.Where(p => p.Detection == DetectionMS.MS || p.Detection == DetectionMS.MSMS).ToList();
            List<Intensity> Peptide2Vals = Peptide2.Where(p => p.Detection == DetectionMS.MS || p.Detection == DetectionMS.MSMS).ToList();

            foreach (string group in groups)
            {
                //require at least three measurements in each group
                if (Peptide1Vals.Where(p => p.GroupID == group).Count() < 3 || Peptide2Vals.Where(p => p.GroupID == group).Count() < 3) 
                {
                    return -3;
                }
            }

            double correlation = 3; 
            foreach (string group in groups)
            {
                IEnumerable<double> Pep1group = Peptide1Vals.Where(p => p.GroupID == group).Select(p => p.IntensityVal);
                IEnumerable<double> Pep2group =  Peptide2Vals.Where(p => p.GroupID == group).Select(p => p.IntensityVal);
                double temp = Correlation.Pearson(Pep1group, Pep2group);
                if (temp < correlation)
                {
                    correlation = temp;
                }
            }

            return correlation;
        }


        /*
        //function to calc Covariance: 1/n * sum[(X-ave(X))(Y-ave(Y))] - TODO: change to correlation so not impacted by scale
        private double Covariance(List<Intensity> Peptide1, List<Intensity> Peptide2, List<string> groups)
        {
            List<Intensity> Peptide1Vals = Peptide1.Where(p => p.Detection == DetectionMS.MS || p.Detection == DetectionMS.MSMS).ToList();
            List<Intensity> Peptide2Vals = Peptide2.Where(p => p.Detection == DetectionMS.MS || p.Detection == DetectionMS.MSMS).ToList();

            foreach (string group in groups)
            {
                if (Peptide1Vals.Where(p => p.GroupID == group).Count() < 3 || Peptide2Vals.Where(p => p.GroupID == group).Count() < 3) //require at least three measurements in each group
                {
                    return -3;  
                }
            }

            Double Pep1Ave = Peptide1Vals.Select(p => p.IntensityVal).Average();
            Double Pep2Ave = Peptide2Vals.Select(p => p.IntensityVal).Average();

            Peptide1Vals.Select(p => p.IntensityVal - Pep1Ave);
            Peptide2Vals.Select(p => p.IntensityVal - Pep2Ave);
            double covariance = 0;
            for (int p1 = 0; p1 < Peptide1Vals.Count(); p1++)
            {
                for (int p2 = 0; p2 < Peptide1Vals.Count(); p2++)
                {
                    covariance += Peptide1Vals[p1].IntensityVal * Peptide2Vals[p2].IntensityVal;
                }
            }

            covariance = covariance / (Peptide1Vals.Count() * Peptide2Vals.Count());

            return covariance;
        }
        */

        //function check whether is useful protein: must have baseline peptides required (3 unmodified peptides) and at least on mod peptide to consider
        private string isProteinUseful(List<Peptide> pepsInProt)
        {
            if (pepsInProt.Count() < 4)
            {
                return "InsufficientPeptides";
            }
            else if (pepsInProt.Where(p => p.BaseSeq == p.Sequence).ToList().Count() < 3)
            {
                return "BaseLineReqNotMet";
            }
            else if (pepsInProt.Where(p => p.BaseSeq != p.Sequence).ToList().Count() == 0)
            {
                return "unmodOnly";
            }

            return "modandunmod";
        }

        //function to get list of intensities of baseline peptides
        private List<Intensity> getBaselineIntensities()
        {
            List<Intensity> BaselinePepIntensity = new List<Intensity>(); //intensities baseline for group of interest
            foreach (Peptide basePep in this.BaselinePeptides)
            {
                foreach (Intensity i2 in basePep.Intensities)
                {
                    BaselinePepIntensity.Add(i2);
                }

            }

            return BaselinePepIntensity;
        }

        //function to compare all modified peptides in protein against baseline using PairwiseCompairison, 
        //if UseRazorPeptides is set to false, peptides that are in more than one protein will be removed
        private List<PairwiseComparison> calcComparison(List<string> groups)
        {
            List<PairwiseComparison> comparePeps = new List<PairwiseComparison>();

            List<Peptide> modPep = this.PeptidesInProtein.Where(p => p.BaseSeq != p.Sequence).ToList();

       
            for (int p1 = 0; p1 < modPep.Count(); ++p1)
            {

                 for (int g1 = 0; g1 < groups.Count(); ++g1)
                 {
                     for (int g2 = (g1 + 1); g2 < groups.Count(); ++g2)
                     {
                            PairwiseComparison temp = new PairwiseComparison(modPep[p1], getBaselineIntensities(), groups[g1], groups[g2]);
                            if (temp.PeptideStoichiometriesGroupOne.Count > 3) //p-value set to -1 if both stoichiometry groups not larger than 3 values 
                            {
                                comparePeps.Add(new PairwiseComparison(modPep[p1], getBaselineIntensities(), groups[g1], groups[g2]));
                            }
                     }
                 }

            }
            return comparePeps;
        }

        //depreciated
        /*
        //calc PairwiseCompairsons for each pair pf peptides and every pair of groups
        private List<PairwiseComparison> calcComparison1(List<string> groups)
        {
            List<PairwiseComparison> comparePeps = new List<PairwiseComparison>();
            
            for (int p1 = 0; p1 < this.PeptidesInProtein.Count(); ++p1)
            {
                for (int p2 = (p1+1); p2 < this.PeptidesInProtein.Count(); ++p2)
                {
                    for (int g1 = 0; g1 < groups.Count(); ++g1)
                    {
                        for (int g2 = (g1+1); g2 < groups.Count(); ++g2)
                        {
                            PairwiseComparison temp = new PairwiseComparison(this.PeptidesInProtein[p1], this.PeptidesInProtein[p2], groups[g1], groups[g2]);
                            if (temp.PeptideStoichiometriesGroupOne.Count > 3 && temp.PeptideStoichiometriesGroupTwo.Count > 3) //p-value set to -1 if both stoichiometry groups not larger than 3 values 
                            {
                                comparePeps.Add(new PairwiseComparison(this.PeptidesInProtein[p1], this.PeptidesInProtein[p2], groups[g1], groups[g2]));
                            }
                        }
                    }
                }
            }
            return comparePeps;
        }
        */

        /*
        public double BenjaminiHochberg(List<PairwiseComparison> Comparisons, double alpha = 0.5)
        {
            List<double> pvals = new List<double>();
            foreach (PairwiseComparison comp in Comparisons)
            {
                pvals.Add(comp.MWPVal);
            }
            //select largest p-value such that: (k+1)*alpha/#pvals > p-val, wherek = # in an ordered list of the p-vals
            pvals.Sort();
            for (int k = 0; k < pvals.Count(); ++k)
            {
                if (pvals[k] > ((k+1) * alpha) / pvals.Count())
                {
                    if (k > 0)
                    {
                        return pvals[k - 1];
                    }
                    else
                    {
                        return pvals[k];
                    }               
                }
            }
            return 0; //this makes nothing sig pvals[pvals.Count() - 1];
        }
        */

        /*
        //calc PairwiseCompairsons for each pair pf peptides and every pair of groups
        private List<PairwiseComparison> calcComparison(Dictionary<string, string> groups)
        {
            List<PairwiseComparison> comparePeps = new List<PairwiseComparison>();
            foreach (Peptide pep1 in this.PeptidesInProtein)
            {
                foreach (Peptide pep2 in this.PeptidesInProtein)
                {
                    foreach (string g in groups.Keys)
                    {
                        foreach (string h in groups.Keys)
                        {
                            comparePeps.Add(new PairwiseComparison(pep1, pep2, groups[g], groups[h]));
                        }
                    }
                }
            }
            return comparePeps;
        }
        */
    }
}
