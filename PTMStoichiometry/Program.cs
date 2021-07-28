﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace PTMStoichiometry
{
    public class Program
    {
        static void Main(string[] args)
        {
            //user parameters - will need to validate input

            //groupToCompare - single group to compare against, this is the group name (e.g. a control group) (default = null)
            //useBaselinePeptides - if true (default) use an averaged baseline of covarying peptides
            //reqNumBaselinePeptides - min num of baseline peptides that must be observed for a protein in order to consider it (default=3)
            //correlationCutOff - min value at which two peptides will be considered to be correlated
            //reqNumOfPepeptides - min num of peptides that must be observed for a protein in order to consider it
            //reqNumModPeptides - min num of modified peptides that must be observed for a protein in order to consider it (default=1)
            //reqNumUnmodPeptides - min num of modified peptides that must be observed for a protein in order to consider it (default=3)
            //reqNumPepMeasurements - min num of peptide intensities that must be observed (non zero -> MS or MSMS detection)
            //reqNumBaselineMeasurements - min num of intensities (non zero -> MS or MSMS detection) that must be observed for in a 
            //baseline peptide (non zero -> MS or MSMS detection) - increasing this value will decrease the number of baseline peptides  
            //that are not observed in samples and therefore the number of non numeric stoichiometry values found in baseline case
            //compareUnmod - if false (default) only compare modified peptides to baseline, not unmodified peptides

            //useRazorPeptides - if false (default) peptides in more than one protein are removed
            //test - currently only MW available, want to add t-test and ANOVA
            
            //minNumStoichiometries - min num of stoichiometries req in both groups before run test

            int reqNumUnmodPeptides = 1;
            int reqNumModPeptides = 1;
            int reqNumOfPepeptides = reqNumUnmodPeptides + reqNumModPeptides;
            bool useBaselinePeptides = true;
            int reqNumBaselinePeptides = 3;
            int reqNumBaselineMeasurements = 3; 
            double correlationCutOff = 0.75;
            bool compareUnmod = false;
            int minNumStoichiometries = 3;
            int reqNumPepMeasurements = 3;

            string groupToCompare = null;
            string dataType = "unknown";

            string filepathpeptides = @"C:\Users\KAP\source\repos\PTMStoichiometry_master\Test\TestData\MSV000086126-2021-07-07-08-59-09-AllQuantifiedPeptides-10000PeptidesProteinAlphabetized.txt";
            string filepathgroups = @"C:\Users\KAP\source\repos\PTMStoichiometry_master\Test\TestData\MSV000086126GlobalGroups.txt";
            string directory = @"D:\PTMStoichiometry\UnitTests\";

            //string filepathpeptides = @"D:\PTMStoichiometry\TestData\PXD003881\2021-07-07-09-11-30\Task3-SearchTask\AllQuantifiedPeptides.tsv";
            //string filepathgroups = @"D:\PTMStoichiometry\TestData\PXD003881\2021-07-07-09-11-30\Task3-SearchTask\PXD003881_MM_Groups.txt";
            //string directory = @"D:\PTMStoichiometry\TestData\PXD003881\2021-07-07-09-11-30\Task3-SearchTask\";

            //string filepathpeptides = @"D:\PTMStoichiometry\TestData\MSV000086126\2021-07-07-08-59-09\Task3-SearchTask\AllQuantifiedPeptides.tsv";
            //string filepathgroups = @"D:\PTMStoichiometry\TestData\MSV000086126\2021-07-07-08-59-09\Task3-SearchTask\MSV000086126GlobalGroups.txt";
            //string directory = @"D:\PTMStoichiometry\TestData\MSV000086126\2021-07-07-08-59-09\Task3-SearchTask\";


            if (File.ReadAllLines(filepathpeptides, Encoding.UTF8)[0].Split("\t")[4] == "Organism")
            {
                dataType = "FlashLFQ";
            }
            else
            {
                dataType = "MaxQuant";
            }
            string subdirectory = "MSV000086126-2021-07-07-08-59-09-AllQuantifiedPeptides-10000PeptidesProteinAlphabetized-20210728a";
            string peptidestoichiometryfileout = subdirectory + "PeptideAnalysis";
            string ptmstoichiometryfileout = subdirectory + "PTMAnalysis";
            string paramsfile = subdirectory + "params";

            int intensityIndex = 5;
            if (dataType == "MaxQuant")
            {
                intensityIndex = PeptideReader.IndexFind(filepathpeptides, "Intensity");
            }


            Dictionary<string, string> groups = PeptideReader.GetGroups(filepathgroups);
            List<string> groupsList = PeptideReader.GetGroupList(filepathgroups);
            List<Peptide> testPeptide = PeptideReader.ReadTsv(filepathpeptides, filepathgroups, reqNumPepMeasurements, intensityIndex, dataType);
            //testPeptide = Extensions.IncludeSharedPeptides(testPeptide, false); 


            //group peptides by protein
            //var proteins = testPeptide.Where(p => p.ProteinGroup.Count() == 1).Select(p => p.ProteinGroup).Distinct().ToArray(); //this is problem line!//TODO: chance of leaving things out? - think it is okay bc if doesn't make it past this has NO unique peps
            var proteins = testPeptide.Where(p => p.ProteinGroup.Count() == 1).Select(p => p.ProteinGroup).Distinct().ToList();

            List<string> proteinList = new List<string>(); //  proteins.Select(p => string p[0] { p = p }).ToList()
            foreach (List<string> prot in proteins)
            {
                proteinList.Add(prot[0]);
            }
            proteinList = proteinList.Distinct().ToList();
            List<ProteinGroup> testProt = new List<ProteinGroup>();
            
            if (groupToCompare != null)
            {
                for (int i = 0; i < proteinList.Count(); i++)
                {
                    testProt.Add(new ProteinGroup(proteinList[i], testPeptide, groupsList, reqNumUnmodPeptides, reqNumModPeptides, reqNumOfPepeptides,
                    useBaselinePeptides, reqNumBaselinePeptides, reqNumBaselineMeasurements, correlationCutOff, compareUnmod, minNumStoichiometries, groupToCompare));
                }
            }
            else
            {
                for (int i = 0; i < proteinList.Count(); i++)
                {
                    testProt.Add(new ProteinGroup(proteinList[i], testPeptide, groupsList, reqNumUnmodPeptides, reqNumModPeptides, reqNumOfPepeptides,
                    useBaselinePeptides, reqNumBaselinePeptides, reqNumBaselineMeasurements, correlationCutOff, compareUnmod, minNumStoichiometries));
                }
            }

            testProt = testProt.Where(p => p.ProteinPairwiseComparisons != null).Distinct().ToList(); //hmmm
            
            List<ProteinGroup> ProteinsToUse = testProt.Where(p => p.useProt).ToList();
            
            WriteFile.ParamsWriter(paramsfile, filepathpeptides, filepathgroups, directory, peptidestoichiometryfileout, reqNumUnmodPeptides, reqNumModPeptides,
                reqNumOfPepeptides, useBaselinePeptides, reqNumBaselinePeptides, reqNumBaselineMeasurements, correlationCutOff, compareUnmod,
                minNumStoichiometries, reqNumPepMeasurements, groupToCompare);
            WriteFile.StoichiometryPeptideDataWriter(ProteinsToUse, useBaselinePeptides, minNumStoichiometries, directory, peptidestoichiometryfileout);
            WriteFile.StoichiometryPTMDataWriter(ProteinsToUse, useBaselinePeptides, minNumStoichiometries, directory, ptmstoichiometryfileout);

        }
    }
}
