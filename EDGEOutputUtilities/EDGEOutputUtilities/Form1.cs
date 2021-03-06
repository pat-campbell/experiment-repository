﻿using System;
using System.Net;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;

namespace WindowsFormsApplication2
{
    public partial class Form1 : Form
    {
        System.Xml.XmlDocument xmlClaims = null;
        string strSavedDownloadFolder = "C:\\Users\\Pat\\Documents\\CoChoice\\EDGE Server";

        public Form1()
        {
            InitializeComponent();
            tbxFindText.Text = "insuredMemberIdentifier>31605<";
        }

        private void btnProcessMvsMD_Click(object sender, EventArgs e)
        {
            StreamWriter StrmWtr_Main;
            Cursor.Current = Cursors.WaitCursor;

            OpenFileDialog OpnFlDlg_MedicalClaims = new OpenFileDialog();
            OpnFlDlg_MedicalClaims.CheckFileExists = true;
            OpnFlDlg_MedicalClaims.Title = "Medical Claims (XML uploaded to ES) file:";
            OpnFlDlg_MedicalClaims.Filter = "XML Files (*.XML)|*.XML";
            OpnFlDlg_MedicalClaims.DefaultExt = ".XML";
            OpnFlDlg_MedicalClaims.AddExtension = false;
            if (OpnFlDlg_MedicalClaims.ShowDialog() != DialogResult.OK)
                return;
            Cursor.Current = Cursors.WaitCursor;

            //LOAD THE CLAIMS FILE INTO AN XmlDocument
            System.Xml.XmlDocument xmlClaims = new System.Xml.XmlDocument();
            xmlClaims.Load(OpnFlDlg_MedicalClaims.FileName);

            //getAllClaims(xmlClaims);
            //return;

            OpenFileDialog OpnFlDlg_MedicalDetail = new OpenFileDialog();
            OpnFlDlg_MedicalDetail.CheckFileExists = true;
            OpnFlDlg_MedicalDetail.Title = "Medical Detail (ES output) file:";
            OpnFlDlg_MedicalDetail.Filter = "XML Files (*.XML)|*.XML";
            OpnFlDlg_MedicalDetail.DefaultExt = ".XML";
            OpnFlDlg_MedicalDetail.AddExtension = false;
            if (OpnFlDlg_MedicalDetail.ShowDialog() != DialogResult.OK)
                return;
            Cursor.Current = Cursors.WaitCursor;

            decimal totalMoney = 0.0M;
            int rejectedClaimCount = 0;
            int totalClaimCount = 0;

            //SETUP OUTPUT FILE
            try
            {
                StrmWtr_Main = new StreamWriter(folderFromFileName(OpnFlDlg_MedicalDetail.FileName) + "\\claims_hybrid.xml", false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open file " + ex.Message);
                return;
            }

            //WALK THROUGH THE DETAIL
            string currentClaimRecordID = null;
            string currentServiceLineRecordID = null;
            string currentClaimID = null;
            string currentClaimStatus = null;
            string currentServiceLineStatus = null;
            string currentClaimOffendingElementName = null;
            string currentServiceLineOffendingElementName = null;
            string currentClaimOffendingElementValue = null;
            string currentServiceLineOffendingElementValue = null;
            string currentClaimOffendingElementErrorTypeMessage = null;
            string currentClaimOffendingElementErrorTypeMessage2 = null;
            string currentServiceLineOffendingElementErrorTypeMessage = null;
            string currentPlanID = null;
            bool bInClaimServiceLine = false;

            ////////////////////////////////////////////////////////////////////////////////////////////////////
            //// OPTION SETTINGS ///////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////
            decimal mMinClaimLine = 500.00m;
            decimal mMinClaim = 50.00m;
            bool bProductionZone = true;
            bool bExcludeKnownRejections = true;
            string strKnownRejections = "1534111056HC,15331E000IHC,1530810115MC,1535710380HC,1521810625HC,1522510521HC,1525111714HC,1520511077HC,15218R0622HC,1522510520HC,1516110278HC,1506110556MC,15089R0585MC";
            ////////////////////////////////////////////////////////////////////////////////////////////////////

            using (XmlReader reader = XmlReader.Create(OpnFlDlg_MedicalDetail.FileName))
            {
                reader.Read();
                while (!reader.EOF)
                {
                    bool bConsumed = false;
                    if (reader.NodeType == XmlNodeType.EndElement)
                        switch (reader.Name.Replace("ns1:", ""))
                        {
                            case "includedClaimProcessingResult":
                                {
                                    bInClaimServiceLine = false;
                                    Text = ++totalClaimCount + " claims, " + rejectedClaimCount + " rejected";
                                    if (currentClaimStatus == "R")
                                    {
                                        //if (currentClaimID == "1506110556MC")
                                        //    currentClaimID = currentClaimID;
                                        if (bExcludeKnownRejections && strKnownRejections.Contains(currentClaimID))
                                            break;

                                        decimal dClaimPaidAmount = 0.0m;
                                        rejectedClaimCount++;

                                        if (currentClaimOffendingElementErrorTypeMessage == "Claim header level rejected because all the claim service for the medical submission failed validation")
                                            break;
                                        if (currentClaimOffendingElementErrorTypeMessage == "Statement Covers Through Date must be within the current payment year(s) accepted by the EDGE server")
                                            break;
                                        if (bProductionZone && currentClaimOffendingElementErrorTypeMessage == "Claim rejected because claimIdentifier already exists in the database")
                                            break;
                                        if (bProductionZone && currentClaimOffendingElementErrorTypeMessage == "Plan level rejected because all the claim header for the medical submission failed validation")
                                            break;

                                        XmlNode thisClaim2 = getClaim(xmlClaims, currentClaimID, currentPlanID);
                                        if (thisClaim2 == null)
                                            StrmWtr_Main.WriteLine("claim not found");
                                        else
                                        {
                                            string strClaim = thisClaim2.InnerXml;
                                            //StrmWtr_Main.WriteLine("policyPaidTotalAmount = " + strClaim.Substring(strClaim.IndexOf("<ns1:policyPaidTotalAmount"), strClaim.IndexOf("</ns1:policyPaidTotalAmount>") - strClaim.IndexOf("<ns1:policyPaidTotalAmount")).Replace("<ns1:policyPaidTotalAmount xmlns:ns1=\"http://vo.edge.fm.cms.hhs.gov\">", ""));
                                            dClaimPaidAmount = decimal.Parse(strClaim.Substring(strClaim.IndexOf("<ns1:policyPaidTotalAmount"), strClaim.IndexOf("</ns1:policyPaidTotalAmount>") - strClaim.IndexOf("<ns1:policyPaidTotalAmount")).Replace("<ns1:policyPaidTotalAmount xmlns:ns1=\"http://vo.edge.fm.cms.hhs.gov\">", ""));
                                            totalMoney += dClaimPaidAmount;
                                            if (dClaimPaidAmount > mMinClaim)
                                                StrmWtr_Main.WriteLine("Paid = " + dClaimPaidAmount + " MemberID = " + strClaim.Substring(strClaim.IndexOf("<ns1:insuredMemberIdentifier"), strClaim.IndexOf("</ns1:insuredMemberIdentifier>") - strClaim.IndexOf("<ns1:insuredMemberIdentifier")).Replace("<ns1:insuredMemberIdentifier xmlns:ns1=\"http://vo.edge.fm.cms.hhs.gov\">", ""));
                                        }

                                        if (dClaimPaidAmount > mMinClaim)
                                        {
                                            StrmWtr_Main.WriteLine("CLAIM REJECTED: " + currentClaimID + " (recordID " + currentClaimRecordID + ")");
                                            if (currentClaimOffendingElementValue == "")
                                                StrmWtr_Main.WriteLine("  " + currentClaimOffendingElementErrorTypeMessage);
                                            else
                                                StrmWtr_Main.WriteLine("  " + currentClaimOffendingElementErrorTypeMessage + " (" + currentClaimOffendingElementName + " = " + currentClaimOffendingElementValue + ")");
                                            if (currentClaimOffendingElementErrorTypeMessage2 != null)
                                                StrmWtr_Main.WriteLine(" and 2nd Error: " + currentClaimOffendingElementErrorTypeMessage2);
                                            if (currentClaimOffendingElementErrorTypeMessage != "Reference check failed" &&
                                                currentClaimOffendingElementErrorTypeMessage != "DischargeStatusCode Required for institutional claim" &&
                                                currentClaimOffendingElementErrorTypeMessage != "Void or replace claim must reference an existing originalClaimIdentifier if the VoidReplaceIndicator is populated as V or R")
                                            {
                                                XmlNode thisClaim = getClaim(xmlClaims, currentClaimID, currentPlanID);
                                                if (thisClaim == null)
                                                    StrmWtr_Main.WriteLine("claim not found");
                                                else
                                                {
                                                    if (currentClaimOffendingElementErrorTypeMessage.StartsWith("Claim Header level rejected because the Statement-covers-from and Statement-covers-to dates are overlapping"))
                                                    {
                                                        string strClaim = thisClaim.InnerXml;
                                                        //StrmWtr_Main.WriteLine("policyPaidTotalAmount = " + strClaim.Substring(strClaim.IndexOf("<ns1:policyPaidTotalAmount"), strClaim.IndexOf("</ns1:policyPaidTotalAmount>") - strClaim.IndexOf("<ns1:policyPaidTotalAmount")).Replace("<ns1:policyPaidTotalAmount xmlns:ns1=\"http://vo.edge.fm.cms.hhs.gov\">", ""));
                                                        decimal dTmp = decimal.Parse(strClaim.Substring(strClaim.IndexOf("<ns1:policyPaidTotalAmount"), strClaim.IndexOf("</ns1:policyPaidTotalAmount>") - strClaim.IndexOf("<ns1:policyPaidTotalAmount")).Replace("<ns1:policyPaidTotalAmount xmlns:ns1=\"http://vo.edge.fm.cms.hhs.gov\">", ""));
                                                        totalMoney += dTmp;
                                                        XmlNode overlappingClaim = getOverlappingClaim(xmlClaims, thisClaim, currentPlanID);
                                                        if (overlappingClaim == null)
                                                            StrmWtr_Main.WriteLine("Overlapping claim not found");
                                                        else
                                                            StrmWtr_Main.WriteLine("Overlapping claim ID: " + overlappingClaim["ns1:claimIdentifier"].InnerText);

                                                    }
                                                    else
                                                        StrmWtr_Main.WriteLine(Beautify(thisClaim.OuterXml));
                                                }
                                            }
                                            StrmWtr_Main.WriteLine("");
                                            //StrmWtr_Main.WriteLine("--------------------------------------------------------------");
                                            StrmWtr_Main.WriteLine("");
                                        }
                                    }
                                    currentClaimOffendingElementErrorTypeMessage = null;
                                    currentClaimOffendingElementErrorTypeMessage2 = null;
                                    break;
                                }
                            case "includedClaimServiceLineProcessingResult":
                                {
                                    bInClaimServiceLine = false;
                                    if (bProductionZone && currentServiceLineOffendingElementErrorTypeMessage == "Claim Service Line level rejected because the claim service line already exists in the database")
                                        break;
                                    if (currentServiceLineStatus == "R" && !currentServiceLineOffendingElementErrorTypeMessage.Contains("Claim Service Line level rejected because the claim header for the medical submission failed validation"))
                                    {
                                        if (bExcludeKnownRejections && strKnownRejections.Contains(currentClaimID))
                                            break;

                                        XmlNode thisClaim = getClaim(xmlClaims, currentClaimID, currentPlanID);
                                        string strClaim = thisClaim.InnerXml;
                                        if (decimal.Parse(strClaim.Substring(strClaim.IndexOf("<ns1:policyPaidTotalAmount"), strClaim.IndexOf("</ns1:policyPaidTotalAmount>") - strClaim.IndexOf("<ns1:policyPaidTotalAmount")).Replace("<ns1:policyPaidTotalAmount xmlns:ns1=\"http://vo.edge.fm.cms.hhs.gov\">", "")) < mMinClaimLine)
                                            break;
                                        StrmWtr_Main.WriteLine("   Service line rejected: " + currentClaimID + " (recordID " + currentServiceLineRecordID + ") paid = " + decimal.Parse(strClaim.Substring(strClaim.IndexOf("<ns1:policyPaidTotalAmount"), strClaim.IndexOf("</ns1:policyPaidTotalAmount>") - strClaim.IndexOf("<ns1:policyPaidTotalAmount")).Replace("<ns1:policyPaidTotalAmount xmlns:ns1=\"http://vo.edge.fm.cms.hhs.gov\">", "")));
                                        if (currentServiceLineOffendingElementValue == "")
                                            StrmWtr_Main.WriteLine("  " + currentServiceLineOffendingElementErrorTypeMessage);
                                        else
                                            StrmWtr_Main.WriteLine("  " + currentServiceLineOffendingElementErrorTypeMessage + " (" + currentServiceLineOffendingElementName + " = " + currentServiceLineOffendingElementValue + ")");
                                        if (currentServiceLineOffendingElementErrorTypeMessage == "Claim Service Line level rejected because the claim service line already exists in the database")
                                        {
                                            XmlNode matchingClaim = getDupServiceLine(xmlClaims, thisClaim, getServiceLine(xmlClaims, currentServiceLineRecordID, currentPlanID), currentPlanID);
                                            if (matchingClaim == null)
                                                StrmWtr_Main.WriteLine("   matching claim line not found");
                                            else
                                                StrmWtr_Main.WriteLine("   matching claim ID: " + matchingClaim["ns1:claimIdentifier"].InnerText);
                                        }
                                        else if (currentServiceLineOffendingElementErrorTypeMessage != "Reference check failed")
                                        {
                                            if (thisClaim == null)
                                                StrmWtr_Main.WriteLine("   claim not found");
                                            else
                                            {
                                                if (currentClaimOffendingElementErrorTypeMessage == "Claim Header level rejected because the Statement-covers-from and Statement-covers-to dates are overlapping within the file or database")
                                                {
                                                    StrmWtr_Main.WriteLine("   policyPaidTotalAmount = " + strClaim.Substring(strClaim.IndexOf("<ns1:policyPaidTotalAmount"), strClaim.IndexOf("</ns1:policyPaidTotalAmount>") - strClaim.IndexOf("<ns1:policyPaidTotalAmount")).Replace("<ns1:policyPaidTotalAmount xmlns:ns1=\"http://vo.edge.fm.cms.hhs.gov\">", ""));
                                                    decimal dTmp = decimal.Parse(strClaim.Substring(strClaim.IndexOf("<ns1:policyPaidTotalAmount"), strClaim.IndexOf("</ns1:policyPaidTotalAmount>") - strClaim.IndexOf("<ns1:policyPaidTotalAmount")).Replace("<ns1:policyPaidTotalAmount xmlns:ns1=\"http://vo.edge.fm.cms.hhs.gov\">", ""));
                                                    totalMoney += dTmp;
                                                }
                                                else
                                                    StrmWtr_Main.WriteLine(Beautify(thisClaim.OuterXml));
                                            }
                                        }
                                        StrmWtr_Main.WriteLine("");
                                        //StrmWtr_Main.WriteLine("   --------------------------------------------------------------");
                                    }
                                    break;
                                }
                        }
                    else if (reader.NodeType == XmlNodeType.Element)
                    {
                        string strElementName = (reader.Name.Contains(" ") ? reader.Name.Remove(reader.Name.IndexOf(" ")) : reader.Name);
                        switch (strElementName.Replace("ns1:", ""))
                        {
                            case "includedClaimServiceLineProcessingResult":
                                {
                                    bInClaimServiceLine = true;
                                    break;
                                }
                            case "medicalClaimRecordIdentifier":
                                {
                                    currentClaimRecordID = reader.ReadElementContentAsString();
                                    bConsumed = true;
                                    break;
                                }
                            case "medicalClaimIdentifier":
                                {
                                    currentClaimID = reader.ReadElementContentAsString();
                                    bConsumed = true;
                                    break;
                                }
                            case "insurancePlanIdentifier":
                                {
                                    currentPlanID = reader.ReadElementContentAsString();
                                    bConsumed = true;
                                    break;
                                }
                            case "medicalClaimServiceLineRecordIdentifier":
                                {
                                    currentServiceLineRecordID = reader.ReadElementContentAsString();
                                    bConsumed = true;
                                    break;
                                }
                            case "statusTypeCode":
                                {
                                    if (bInClaimServiceLine)
                                        currentServiceLineStatus = reader.ReadElementContentAsString();
                                    else
                                        currentClaimStatus = reader.ReadElementContentAsString();
                                    bConsumed = true;
                                    break;
                                }
                            case "offendingElementName":
                                {
                                    if (bInClaimServiceLine)
                                        currentServiceLineOffendingElementName = reader.ReadElementContentAsString();
                                    else
                                        currentClaimOffendingElementName = reader.ReadElementContentAsString();
                                    bConsumed = true;
                                    break;
                                }
                            case "offendingElementValue":
                                {
                                    if (bInClaimServiceLine)
                                        currentServiceLineOffendingElementValue = reader.ReadElementContentAsString();
                                    else
                                        currentClaimOffendingElementValue = reader.ReadElementContentAsString();
                                    bConsumed = true;
                                    break;
                                }
                            case "offendingElementErrorTypeMessage":
                                {
                                    if (bInClaimServiceLine)
                                        currentServiceLineOffendingElementErrorTypeMessage = reader.ReadElementContentAsString();
                                    else
                                        if (currentClaimOffendingElementErrorTypeMessage != null)
                                            currentClaimOffendingElementErrorTypeMessage2 = reader.ReadElementContentAsString();
                                        else
                                            currentClaimOffendingElementErrorTypeMessage = reader.ReadElementContentAsString();
                                    bConsumed = true;
                                    break;
                                }
                        }
                    }
                    if (!bConsumed)
                        reader.Read();
                }
            }

            //SaveFileDialog saveFileDialog = new SaveFileDialog();
            //saveFileDialog.AddExtension = true;
            //saveFileDialog.CheckPathExists = true;
            //saveFileDialog.DefaultExt = ".txt";
            //saveFileDialog.Filter = "Text Files (*.txt)|*.txt";
            //saveFileDialog.FileName = Program.currentDataFile + " flat.txt";
            ////saveFileDialog.InitialDirectory = Application.StartupPath + "\\util";
            //saveFileDialog.OverwritePrompt = true;
            //saveFileDialog.RestoreDirectory = true;
            //saveFileDialog.Title = "Name of Output File:";
            //saveFileDialog.ValidateNames = true;
            //if (saveFileDialog.ShowDialog() != DialogResult.OK)
            //    return;
            //DataExport.exportToFlatFile(saveFileDialog.FileName);
            //StreamWriter StrmWtr_Main = null;

            StrmWtr_Main.WriteLine("total money = " + totalMoney);

            StrmWtr_Main.WriteLine("count of rejected claims = " + rejectedClaimCount);

            try
            {
                StrmWtr_Main.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to write/close file" + ex.Message);
                return;
            }
        }

        static public string Beautify(XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.NewLineChars = "\r\n";
            settings.NewLineHandling = NewLineHandling.Replace;
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }
            return sb.ToString();
        }

        static public string Beautify(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.NewLineChars = "\r\n";
            settings.NewLineHandling = NewLineHandling.Replace;
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }
            return sb.ToString();
        }


        private XmlNode getClaim(XmlDocument doc, string claimID, string planID)
        {
            foreach (XmlNode plan in doc.DocumentElement.ChildNodes[8].ChildNodes)
                if (plan.Name == "ns1:includedMedicalClaimPlan")
                {
                    foreach (XmlNode node in plan.ChildNodes)
                        if (plan.ChildNodes[1].InnerText == planID)
                            foreach (XmlNode claim in plan.ChildNodes)
                                if (claim.Name == "ns1:includedMedicalClaimDetail")
                                    if (claim.InnerXml.Contains(claimID))
                                        return claim;
                }
            return null;
        }

        private XmlNode getDupClaim(XmlDocument doc, XmlNode pClaim, string planID)
        {
            foreach (XmlNode plan in doc.DocumentElement.ChildNodes[8].ChildNodes)
                if (plan.Name == "ns1:includedMedicalClaimPlan")
                {
                    foreach (XmlNode node in plan.ChildNodes)
                        if (plan.ChildNodes[1].InnerText == planID)
                            foreach (XmlNode claim in plan.ChildNodes)
                                if (claim.Name == "ns1:includedMedicalClaimDetail")
                                {
                                    if (claim["ns1:statementCoverFromDate"].InnerText == pClaim["ns1:statementCoverFromDate"].InnerText &&
                                        claim["ns1:statementCoverToDate"].InnerText == pClaim["ns1:statementCoverToDate"].InnerText &&
                                        claim["ns1:insuredMemberIdentifier"].InnerText == pClaim["ns1:insuredMemberIdentifier"].InnerText &&
                                        claim["ns1:formTypeCode"].InnerText == pClaim["ns1:formTypeCode"].InnerText &&
                                        claim["ns1:billingProviderIDQualifier"].InnerText == pClaim["ns1:billingProviderIDQualifier"].InnerText &&
                                        claim["ns1:billingProviderIdentifier"].InnerText == pClaim["ns1:billingProviderIdentifier"].InnerText &&
                                        claim["ns1:claimIdentifier"].InnerText != pClaim["ns1:claimIdentifier"].InnerText)
                                        return claim;
                                }
                }
            return null;
        }

        private XmlNode getOverlappingClaim(XmlDocument doc, XmlNode pClaim, string planID)
        {
            foreach (XmlNode plan in doc.DocumentElement.ChildNodes[8].ChildNodes)
                if (plan.Name == "ns1:includedMedicalClaimPlan")
                {
                    foreach (XmlNode node in plan.ChildNodes)
                        if (plan.ChildNodes[1].InnerText == planID)
                            foreach (XmlNode claim in plan.ChildNodes)
                                if (claim.Name == "ns1:includedMedicalClaimDetail")
                                {
                                    //IF FROMDATE OR TODATE OF PCLAIM IS BETWEEN FROMDATE AND TODATE OF CLAIM OR VICE VERSA
                                    DateTime dtFrom = DateTime.Parse(claim["ns1:statementCoverFromDate"].InnerText);
                                    DateTime dtTo = DateTime.Parse(claim["ns1:statementCoverToDate"].InnerText);
                                    DateTime dtFromP = DateTime.Parse(pClaim["ns1:statementCoverFromDate"].InnerText);
                                    DateTime dtToP = DateTime.Parse(pClaim["ns1:statementCoverToDate"].InnerText);
                                    //IN OTHER WORDS
                                    //IF ((dtFromP >= dtFrom AND dtFromP <= dtTo) OR (dtToP >= dtFrom AND dtToP <= dtTo)) OR
                                    //   ((dtFrom >= dtFromP AND dtFrom <= dtToP) OR (dtTo >= dtFromP AND dtTo <= dtToP))
                                    if ((((dtFromP >= dtFrom && dtFromP <= dtTo) || (dtToP >= dtFrom && dtToP <= dtTo)) ||
                                       ((dtFrom >= dtFromP && dtFrom <= dtToP) || (dtTo >= dtFromP && dtTo <= dtToP))) &&
                                        claim["ns1:insuredMemberIdentifier"].InnerText == pClaim["ns1:insuredMemberIdentifier"].InnerText &&
                                        claim["ns1:formTypeCode"].InnerText == "I" &&
                                        claim["ns1:claimIdentifier"].InnerText != pClaim["ns1:claimIdentifier"].InnerText)
                                        return claim;
                                }
                }
            return null;
        }

        private XmlNode getDupServiceLine(XmlDocument doc, XmlNode pClaim, XmlNode pServiceLine, string planID)
        {
            //FIRST CHECK THE CLAIM ITSELF, AS AN OPTIMIZATION:
            foreach (XmlNode serviceLine in pClaim["ns1:includedDetailServiceLine"].ChildNodes)
                if (serviceLine["ns1:serviceFromDate"].InnerText == pServiceLine["ns1:serviceFromDate"].InnerText &&
                    serviceLine["ns1:serviceToDate"].InnerText == pServiceLine["ns1:serviceToDate"].InnerText &&
                    serviceLine["ns1:revenueCode"].InnerText == pServiceLine["ns1:revenueCode"].InnerText &&
                    serviceLine["ns1:serviceTypeCode"].InnerText == pServiceLine["ns1:serviceTypeCode"].InnerText &&
                    serviceLine["ns1:serviceCode"].InnerText == pServiceLine["ns1:serviceCode"].InnerText &&
                    serviceLine["ns1:serviceModifierCode"].InnerText == pServiceLine["ns1:serviceModifierCode"].InnerText &&
                    serviceLine["ns1:serviceFacilityTypeCode"].InnerText == pServiceLine["ns1:serviceFacilityTypeCode"].InnerText &&
                    serviceLine["ns1:renderingProviderIDQualifier"].InnerText == pServiceLine["ns1:renderingProviderIDQualifier"].InnerText &&
                    serviceLine["ns1:renderingProviderIdentifier"].InnerText == pServiceLine["ns1:renderingProviderIdentifier"].InnerText &&
                    serviceLine["ns1:serviceFacilityTypeCode"].InnerText == pServiceLine["ns1:serviceFacilityTypeCode"].InnerText &&
                    serviceLine["ns1:recordIdentifier"].InnerText != pServiceLine["ns1:recordIdentifier"].InnerText)
                    return pClaim;
            foreach (XmlNode plan in doc.DocumentElement.ChildNodes[8].ChildNodes)
                if (plan.Name == "ns1:includedMedicalClaimPlan")
                {
                    foreach (XmlNode node in plan.ChildNodes)
                        if (plan.ChildNodes[1].InnerText == planID)
                            foreach (XmlNode claim in plan.ChildNodes)
                                if (claim.Name == "ns1:includedMedicalClaimDetail")
                                    foreach (XmlNode serviceLine in claim["ns1:includedDetailServiceLine"].ChildNodes)
                                        if (claim["ns1:insuredMemberIdentifier"].InnerText == pClaim["ns1:insuredMemberIdentifier"].InnerText &&
                                            serviceLine["ns1:serviceFromDate"].InnerText == pServiceLine["ns1:serviceFromDate"].InnerText &&
                                            serviceLine["ns1:serviceToDate"].InnerText == pServiceLine["ns1:serviceToDate"].InnerText &&
                                            serviceLine["ns1:revenueCode"].InnerText == pServiceLine["ns1:revenueCode"].InnerText &&
                                            serviceLine["ns1:serviceTypeCode"].InnerText == pServiceLine["ns1:serviceTypeCode"].InnerText &&
                                            serviceLine["ns1:serviceCode"].InnerText == pServiceLine["ns1:serviceCode"].InnerText &&
                                            serviceLine["ns1:serviceModifierCode"].InnerText == pServiceLine["ns1:serviceModifierCode"].InnerText &&
                                            serviceLine["ns1:serviceFacilityTypeCode"].InnerText == pServiceLine["ns1:serviceFacilityTypeCode"].InnerText &&
                                            serviceLine["ns1:renderingProviderIDQualifier"].InnerText == pServiceLine["ns1:renderingProviderIDQualifier"].InnerText &&
                                            serviceLine["ns1:renderingProviderIdentifier"].InnerText == pServiceLine["ns1:renderingProviderIdentifier"].InnerText &&
                                            serviceLine["ns1:serviceFacilityTypeCode"].InnerText == pServiceLine["ns1:serviceFacilityTypeCode"].InnerText &&
                                            claim["ns1:claimIdentifier"].InnerText != pClaim["ns1:claimIdentifier"].InnerText)
                                            return claim;
                }
            return null;
        }

        private XmlNode getServiceLine(XmlDocument doc, string recordID, string planID)
        {
            foreach (XmlNode plan in doc.DocumentElement.ChildNodes[8].ChildNodes)
                if (plan.Name == "ns1:includedMedicalClaimPlan")
                {
                    foreach (XmlNode node in plan.ChildNodes)
                        if (plan.ChildNodes[1].InnerText == planID)
                            foreach (XmlNode claim in plan.ChildNodes)
                                if (claim.Name == "ns1:includedMedicalClaimDetail")
                                    foreach (XmlNode serviceLine in claim["ns1:includedDetailServiceLine"].ChildNodes)
                                        if (serviceLine["ns1:recordIdentifier"].InnerText == recordID)
                                            return serviceLine;
                }
            return null;
        }
        /// <summary>
        /// POPULATE THE TEXT BOX WITH ALL CLAIMS THAT MATCH THE CRITERIA CODED INTO THIS ROUTINE
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        private XmlNode getAllClaims(XmlDocument doc)
        {
            textBox1.Text = "";
            foreach (XmlNode plan in doc.DocumentElement.ChildNodes[8].ChildNodes)
                if (plan.Name == "ns1:includedMedicalClaimPlan")
                {
                    //foreach (XmlNode node in plan.ChildNodes)
                    //if (plan.ChildNodes[1].InnerText == planID)
                    foreach (XmlNode claim in plan.ChildNodes)
                        if (claim.Name == "ns1:includedMedicalClaimDetail")
                        {
                            string x = claim.InnerXml.Replace(" xmlns:ns1=\"http://vo.edge.fm.cms.hhs.gov\"", "").Replace("ns1:", "");
                            if (
                                //x.Contains("renderingProviderIDQualifier>99<") &&
                                //x.Contains("serviceFacilityTypeCode>11") &&
                                //x.Contains("serviceModifierCode><") &&
                                //x.Contains("serviceCode>90460") &&
                                //x.Contains("serviceTypeCode>03") &&
                                //x.Contains("serviceToDate>2014-03-14") &&
                                //x.Contains("serviceFromDate>2014-03-14") &&
                                x.Contains("insuredMemberIdentifier>70230<") &&
                                x.Contains("serviceFromDate>2015-05-20<") &&
                                x.Contains("serviceToDate>2015-05-20<") &&
                                x.Contains("serviceCode>99213<") &&
                                x.Contains("serviceModifierCode><") &&
                                x.Contains("serviceFacilityTypeCode>11<") &&
                                x.Contains("renderingProviderIDQualifier>99<") &&
                                x.Contains("renderingProviderIdentifier>S0647801<"))
                                textBox1.Text += Beautify("<R>" + x + "</R>") + Environment.NewLine + Environment.NewLine;
                        }
                }
            return null;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog OpnFlDlg = new OpenFileDialog();
            OpnFlDlg.CheckFileExists = true;
            OpnFlDlg.Title = "Input file:";
            OpnFlDlg.Filter = "XML Files (*.XML)|*.XML";
            OpnFlDlg.DefaultExt = ".XML";
            OpnFlDlg.AddExtension = false;
            if (OpnFlDlg.ShowDialog() != DialogResult.OK)
                return;
            Cursor.Current = Cursors.WaitCursor;

            //LOAD THE FILE INTO AN XmlDocument
            System.Xml.XmlDocument xml = new System.Xml.XmlDocument();
            xml.Load(OpnFlDlg.FileName);

            StreamWriter StrmWtr_Main = new StreamWriter(OpnFlDlg.FileName.Replace(".xml", ".beauty.xml").Replace(".XML", ".beauty.xml"));
            StrmWtr_Main.Write(Beautify(xml));
            StrmWtr_Main.Close();
        }

        private void button1_MouseHover(object sender, EventArgs e)
        {
            textBox1.Text = "This will prompt for two XML files: ESMCS EDGE server input file; and the resulting MD detail output report.  It will then write to claims_hybrid.xml (in folder containing MD file) a report containing rejected claims with the reasons for rejection.";
        }

        private void button2_MouseHover(object sender, EventArgs e)
        {
            textBox1.Text = "This will prompt for a single XML file, and will output a file in the same folder with .beauty.xml instead of .xml.";
        }

        /// <summary>
        /// GIVEN A FULL WINDOWS PATH/FILENAME, RETURN JUST THE (COMPLETE) FOLDER NAME
        /// </summary>
        /// <param name="pathname"></param>
        /// <returns></returns>
        public static string folderFromFileName(string pathname)
        {
            return pathname.Remove(pathname.LastIndexOf("\\"));
        }

        //private void textBox1_MouseHover(object sender, EventArgs e)
        //{
        //    textBox1.Text = "Running in " + Application.StartupPath;
        //}

        private void btnDownload_Click(object sender, EventArgs e)
        {
            string[] astrFileNames = new string[] { "RAPHCCER", "CEFR", "EH", "ED", "ES", "MH", "MD", "MS", "PH", "PD", "PS", "RACSD", "RACSS", "RADVPS", "RATEE", "RARSS", "RARSD", "RAUF", "ECD", "ECS", "RISR", "RISD", "RIDE", "FDEEAF", "FDEMAF", "FDEPAF", "FDESAF" };
            string strRootPath = "https://s3.amazonaws.com/cms.edge.ingest.outbox.809141463943/63312/63312.";
            WebClient wc = new WebClient();
            Cursor.Current = Cursors.WaitCursor;
            System.Windows.Forms.FolderBrowserDialog FolderDlg = new FolderBrowserDialog();
            FolderDlg.SelectedPath = strSavedDownloadFolder;
            FolderDlg.ShowDialog();
            if (FolderDlg.SelectedPath == null || FolderDlg.SelectedPath == "")
                return;
            foreach (string fn in astrFileNames)
            {
                try
                {
                    string strTmpFN1 = FolderDlg.SelectedPath + "/" + fn + ".tmp";
                    string strTmpFN2 = FolderDlg.SelectedPath + "/" + fn + ".tmp2";
                    Uri uri = new Uri(strRootPath + fn);
                    wc.DownloadFile(uri, strTmpFN1);
                    System.Xml.XmlDocument xml = new System.Xml.XmlDocument();
                    xml.Load(strTmpFN1);
                    System.Threading.Thread.Sleep(100);
                    string strTemp = Beautify(xml);
                    StreamWriter StrmWtr_Main = new StreamWriter(strTmpFN2);
                    System.Threading.Thread.Sleep(100);
                    foreach (char c in strTemp)
                        StrmWtr_Main.Write(c);
                    StrmWtr_Main.Close();
                    System.Threading.Thread.Sleep(100);
                    FixHeader(strTmpFN2);
                }
                catch (Exception ex)
                {
                }
            }
        }

        /// <summary>
        /// JUST REMOVES ENCODING FROM THE HEADER - TRIED MORE STRAIGHTFORWARD AND SIMPLE WAYS AND string.Replace() WAS RETURING EMPTY STRING!
        /// </summary>
        /// <param name="strTmpFN2"></param>
        private void FixHeader(string strTmpFN2)
        {
            StreamWriter StrmWtr_Main = new StreamWriter(strTmpFN2.Replace(".tmp2", ".xml"));
            StrmWtr_Main.WriteLine("<?xml version=\"1.0\" ?>");
            StreamReader StrmRdr_Main = new StreamReader(strTmpFN2);
            StrmRdr_Main.ReadLine();
            while (!StrmRdr_Main.EndOfStream)
                StrmWtr_Main.WriteLine(StrmRdr_Main.ReadLine());
            StrmRdr_Main.Close();
            StrmWtr_Main.Close();
        }

        private void btnDownload_MouseHover(object sender, EventArgs e)
        {
            textBox1.Text = "Prior to clicking, files in the outbox S3 bucket should be renamed, removing everything starting with .D as in Date (example result: 63312.MD) and they should be made public.  Click this button to download all such files (you'll be prompted for destination folder) beautifying each, and adding .xml extension.  Expect this to take a while.";
        }

        private void btnGetAllClaims_Click(object sender, EventArgs e)
        {


            Cursor.Current = Cursors.WaitCursor;
            //if (xmlClaims == null)
            //{
            OpenFileDialog OpnFlDlg_MedicalClaims = new OpenFileDialog();
            OpnFlDlg_MedicalClaims.CheckFileExists = true;
            OpnFlDlg_MedicalClaims.Title = "Medical Claims file:";
            OpnFlDlg_MedicalClaims.Filter = "XML Files (*.XML)|*.XML";
            OpnFlDlg_MedicalClaims.DefaultExt = ".XML";
            OpnFlDlg_MedicalClaims.AddExtension = false;
            if (OpnFlDlg_MedicalClaims.ShowDialog() != DialogResult.OK)
                return;
            Cursor.Current = Cursors.WaitCursor;

            //LOAD THE CLAIMS FILE INTO AN XmlDocument
            xmlClaims = new System.Xml.XmlDocument();
            xmlClaims.Load(OpnFlDlg_MedicalClaims.FileName);
            //}

            getAllClaims(xmlClaims);///////////////////////////////////////////////////////////////////////////////
            return;////////////////////////////////////////////////////////////////////////////////////

            string strStringToMatch = tbxFindText.Text;

            decimal AllowedAmount = 0.0m;
            decimal PaidAmount = 0.0m;
            decimal OuterAllowedAmount = 0.0m;
            decimal OuterPaidAmount = 0.0m;

            textBox1.Text = "";
            foreach (XmlNode plan in xmlClaims.DocumentElement.ChildNodes[8].ChildNodes)
                if (plan.Name == "ns1:includedMedicalClaimPlan")
                {
                    //foreach (XmlNode node in plan.ChildNodes)
                    //if (plan.ChildNodes[1].InnerText == planID)
                    foreach (XmlNode claim in plan.ChildNodes)
                        if (claim.Name == "ns1:includedMedicalClaimDetail")
                        {
                            string x = claim.InnerXml.Replace(" xmlns:ns1=\"http://vo.edge.fm.cms.hhs.gov\"", "").Replace("ns1:", "");
                            //if (x.Contains(strStringToMatch))
                            if (x.Contains("insuredMemberIdentifier>68250<"))
                            {
                                //textBox1.Text += Beautify("<R>" + x + "</R>") + Environment.NewLine + Environment.NewLine;
                                foreach (XmlNode node in claim.ChildNodes)
                                {
                                    if (node.Name.Replace("ns1:", "") == "claimIdentifier")
                                        textBox1.Text += node.InnerText + " ";
                                    if (node.InnerXml == null)
                                        continue;
                                    if (node.Name.Replace("ns1:", "") == "allowedTotalAmount")
                                        OuterAllowedAmount += decimal.Parse(node.InnerXml);
                                    if (node.Name.Replace("ns1:", "") == "policyPaidTotalAmount")
                                    {
                                        OuterPaidAmount += decimal.Parse(node.InnerXml);
                                        textBox1.Text += node.InnerXml + Environment.NewLine;
                                    }
                                    //foreach (XmlNode subnode in node.ChildNodes)
                                    //{
                                    //    if (subnode.Name.Replace("ns1:", "") == "includedServiceLine")
                                    //        foreach (XmlNode subsubnode in subnode.ChildNodes)
                                    //        {
                                    //            if (subsubnode.InnerXml == null || subsubnode.InnerXml == "")
                                    //                continue;
                                    //            if (subsubnode.Name.Replace("ns1:", "") == "allowedAmount")
                                    //                AllowedAmount += decimal.Parse(subsubnode.InnerXml);
                                    //            if (subsubnode.Name.Replace("ns1:", "") == "policyPaidAmount")
                                    //            {
                                    //                PaidAmount += decimal.Parse(subsubnode.InnerXml);
                                    //                //textBox1.Text += Environment.NewLine + subsubnode.InnerXml;
                                    //            }
                                    //        }
                                    //}
                                }
                            }

                        }
                }
            //textBox1.Text = "OuterAllowedAmount = " + OuterAllowedAmount.ToString("C") + ", OuterPaidAmount = " + OuterPaidAmount.ToString("C") + " AllowedAmount = " + AllowedAmount.ToString("C") + ", PaidAmount = " + PaidAmount.ToString("C");
            Cursor.Current = Cursors.Default;
        }

        private void GetAllPharmClaims()
        {
            Cursor.Current = Cursors.WaitCursor;
            OpenFileDialog OpnFlDlg_MedicalClaims = new OpenFileDialog();
            OpnFlDlg_MedicalClaims.CheckFileExists = true;
            OpnFlDlg_MedicalClaims.Title = "Pharmacy Claims file:";
            OpnFlDlg_MedicalClaims.Filter = "XML Files (*.XML)|*.XML";
            OpnFlDlg_MedicalClaims.DefaultExt = ".XML";
            OpnFlDlg_MedicalClaims.AddExtension = false;
            if (OpnFlDlg_MedicalClaims.ShowDialog() != DialogResult.OK)
                return;
            Cursor.Current = Cursors.WaitCursor;

            //LOAD THE CLAIMS FILE INTO AN XmlDocument
            xmlClaims = new System.Xml.XmlDocument();
            xmlClaims.Load(OpnFlDlg_MedicalClaims.FileName);

            string strStringToMatch = tbxFindText.Text;

            decimal AllowedAmount = 0.0m;
            decimal PaidAmount = 0.0m;
            decimal OuterAllowedAmount = 0.0m;
            decimal OuterPaidAmount = 0.0m;

            textBox1.Text = "";
            foreach (XmlNode plan in xmlClaims.DocumentElement.ChildNodes[7].ChildNodes)
                if (plan.Name == "ns1:includedPharmacyClaimInsurancePlan")
                {
                    //foreach (XmlNode node in plan.ChildNodes)
                    //if (plan.ChildNodes[1].InnerText == planID)
                    foreach (XmlNode claim in plan.ChildNodes)
                        if (claim.Name == "ns1:includedPharmacyClaimDetail")
                        {
                            string x = claim.InnerXml.Replace(" xmlns:ns1=\"http://vo.edge.fm.cms.hhs.gov\"", "").Replace("ns1:", "");
                            if (x.Contains(strStringToMatch))
                            {
                                //textBox1.Text += Beautify("<R>" + x + "</R>") + Environment.NewLine + Environment.NewLine;
                                foreach (XmlNode node in claim.ChildNodes)
                                {
                                    if (node.InnerXml == null)
                                        continue;
                                    if (node.Name.Replace("ns1:", "") == "claimIdentifier")
                                    {
                                        //OuterPaidAmount += decimal.Parse(node.InnerXml);
                                        textBox1.Text += node.InnerXml + " ";
                                    }
                                    if (node.Name.Replace("ns1:", "") == "policyPaidAmount")
                                    {
                                        OuterPaidAmount += decimal.Parse(node.InnerXml);
                                        textBox1.Text += node.InnerXml + Environment.NewLine;
                                    }
                                    //foreach (XmlNode subnode in node.ChildNodes)
                                    //{
                                    //    if (subnode.InnerXml == null || subnode.InnerXml == "")
                                    //        continue;
                                    //    if (subnode.Name.Replace("ns1:", "") == "allowedTotalCostAmount")
                                    //        AllowedAmount += decimal.Parse(subnode.InnerXml);
                                    //    if (subnode.Name.Replace("ns1:", "") == "policyPaidAmount")
                                    //    {
                                    //        PaidAmount += decimal.Parse(subnode.InnerXml);
                                    //        //textBox1.Text += Environment.NewLine + subsubnode.InnerXml;
                                    //    }
                                    //}
                                }
                            }

                        }
                }
            //textBox1.Text = "OuterAllowedAmount = " + OuterAllowedAmount.ToString("C") + ", OuterPaidAmount = " + OuterPaidAmount.ToString("C") + " AllowedAmount = " + AllowedAmount.ToString("C") + ", PaidAmount = " + PaidAmount.ToString("C");
            Cursor.Current = Cursors.Default;
        }

        void makeSpreadsheetFromRIDE()
        {
            OpenFileDialog OpnFlDlg_RIDE = new OpenFileDialog();
            OpnFlDlg_RIDE.CheckFileExists = true;
            OpnFlDlg_RIDE.Title = "RIDE file:";
            OpnFlDlg_RIDE.Filter = "XML Files (*.XML)|*.XML";
            OpnFlDlg_RIDE.DefaultExt = ".XML";
            OpnFlDlg_RIDE.AddExtension = false;
            if (OpnFlDlg_RIDE.ShowDialog() != DialogResult.OK)
                return;
            Cursor.Current = Cursors.WaitCursor;

            //LOAD THE CLAIMS FILE INTO AN XmlDocument
            XmlDocument xmlRIDE = new System.Xml.XmlDocument();
            xmlRIDE.Load(OpnFlDlg_RIDE.FileName);

            StreamWriter StrmWtr_Main;
            Cursor.Current = Cursors.WaitCursor;


            OpenFileDialog OpnFlDlg_out = new OpenFileDialog();
            OpnFlDlg_out.CheckFileExists = false;
            OpnFlDlg_out.Title = "Output file:";
            OpnFlDlg_out.Filter = "Text Files (*.txt)|*.txt";
            OpnFlDlg_out.DefaultExt = ".txt";
            OpnFlDlg_out.AddExtension = false;
            if (OpnFlDlg_out.ShowDialog() != DialogResult.OK)
                return;
            Cursor.Current = Cursors.WaitCursor;

            decimal totalMoney = 0.0M;
            int iCount = 0;

            //SETUP OUTPUT FILE
            try
            {
                StrmWtr_Main = new StreamWriter(OpnFlDlg_out.FileName, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open file " + ex.Message);
                return;
            }

            foreach (XmlNode node in xmlRIDE.DocumentElement.ChildNodes)
                if (node.Name == "includedInsuredMemberIdentifier")
                {
                    string str = node.ChildNodes[0].InnerText;
                    foreach (XmlNode subnode in node.ChildNodes)
                        if (subnode.Name == "includedPlanIdentifier")
                            foreach (XmlNode subnode2 in subnode.ChildNodes)
                                if (subnode2.Name == "includedClaimIdentifier")
                                {
                                    if (subnode2.ChildNodes[0].InnerText == "")
                                        StrmWtr_Main.WriteLine(str + ",n/a,n/a");
                                    else
                                    {
                                        StrmWtr_Main.WriteLine(str + "," + subnode2.ChildNodes[0].InnerText + "," + subnode2.ChildNodes[1].InnerText);
                                        iCount++;
                                    }
                                }
                }
            StrmWtr_Main.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            makeSpreadsheetFromRIDE();
        }

        private void btnGetRIEligibleClaims_Click(object sender, EventArgs e)
        {
            OpenFileDialog OpnFlDlg_claims = new OpenFileDialog();
            OpnFlDlg_claims.CheckFileExists = true;
            OpnFlDlg_claims.Title = "Claims (M) file:";
            OpnFlDlg_claims.Filter = "XML Files (*.XML)|*.XML";
            OpnFlDlg_claims.DefaultExt = ".XML";
            OpnFlDlg_claims.AddExtension = false;
            if (OpnFlDlg_claims.ShowDialog() != DialogResult.OK)
                return;
            Cursor.Current = Cursors.WaitCursor;

            //LOAD THE CLAIMS FILE INTO AN XmlDocument
            XmlDocument xmlClaims = new System.Xml.XmlDocument();
            xmlClaims.Load(OpnFlDlg_claims.FileName);

            StreamWriter StrmWtr_Main;
            Cursor.Current = Cursors.WaitCursor;


            OpenFileDialog OpnFlDlg_out = new OpenFileDialog();
            OpnFlDlg_out.CheckFileExists = false;
            OpnFlDlg_out.Title = "Output file:";
            OpnFlDlg_out.Filter = "Text Files (*.txt)|*.txt";
            OpnFlDlg_out.DefaultExt = ".txt";
            OpnFlDlg_out.AddExtension = false;
            if (OpnFlDlg_out.ShowDialog() != DialogResult.OK)
                return;
            Cursor.Current = Cursors.WaitCursor;

            decimal totalMoney = 0.0M;
            int iCount = 0;

            //SETUP OUTPUT FILE
            try
            {
                StrmWtr_Main = new StreamWriter(folderFromFileName(OpnFlDlg_out.FileName), false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open file " + ex.Message);
                return;
            }

            //foreach (XmlNode node in xmlClaims.DocumentElement.ChildNodes)
            //    if (node.Name == "includedInsuredMemberIdentifier")
            //    {
            //        string str = node.ChildNodes[0].InnerText;
            //        foreach (XmlNode subnode in node.ChildNodes)
            //            if (subnode.Name == "includedPlanIdentifier")
            //                foreach (XmlNode subnode2 in subnode.ChildNodes)
            //                    if (subnode2.Name == "includedClaimIdentifier")
            //                    {
            //                        if (subnode2.ChildNodes[0].InnerText == "")
            //                            StrmWtr_Main.WriteLine(str + ",n/a,n/a");
            //                        else
            //                        {
            //                            StrmWtr_Main.WriteLine(str + "," + subnode2.ChildNodes[0].InnerText + "," + subnode2.ChildNodes[1].InnerText);
            //                            iCount++;
            //                        }
            //                    }
            //    }
            StrmWtr_Main.Close();
        }

        private void btnTemp_Click(object sender, EventArgs e)
        {
            //OpenFileDialog OpnFlDlg_claims = new OpenFileDialog();
            //OpnFlDlg_claims.CheckFileExists = true;
            //OpnFlDlg_claims.Title = "Claims (M) file:";
            //OpnFlDlg_claims.Filter = "XML Files (*.XML)|*.XML";
            //OpnFlDlg_claims.DefaultExt = ".XML";
            //OpnFlDlg_claims.AddExtension = false;
            //if (OpnFlDlg_claims.ShowDialog() != DialogResult.OK)
            //    return;
            //Cursor.Current = Cursors.WaitCursor;

            ////LOAD THE CLAIMS FILE INTO AN XmlDocument
            //XmlDocument xmlClaims = new System.Xml.XmlDocument();
            //xmlClaims.Load(OpnFlDlg_claims.FileName);

            OpenFileDialog OpnFlDlg_RACSD = new OpenFileDialog();
            OpnFlDlg_RACSD.CheckFileExists = true;
            OpnFlDlg_RACSD.Title = "RACSD file:";
            OpnFlDlg_RACSD.Filter = "XML Files (*.XML)|*.XML";
            OpnFlDlg_RACSD.DefaultExt = ".XML";
            OpnFlDlg_RACSD.AddExtension = false;
            if (OpnFlDlg_RACSD.ShowDialog() != DialogResult.OK)
                return;
            Cursor.Current = Cursors.WaitCursor;

            //LOAD THE RACSD FILE INTO AN XMLDOCUMENT
            XmlDocument xmlRACSD = new System.Xml.XmlDocument();
            xmlRACSD.Load(OpnFlDlg_RACSD.FileName);

            //OpenFileDialog OpnFlDlg_out = new OpenFileDialog();
            //OpnFlDlg_out.CheckFileExists = false;
            //OpnFlDlg_out.Title = "Output file:";
            //OpnFlDlg_out.Filter = "Text Files (*.txt)|*.txt";
            //OpnFlDlg_out.DefaultExt = ".txt";
            //OpnFlDlg_out.AddExtension = false;
            //if (OpnFlDlg_out.ShowDialog() != DialogResult.OK)
            //    return;
            Cursor.Current = Cursors.WaitCursor;

            decimal totalMoney = 0.0M;
            string strPaid = "";
            int iCount = 0;

            ////SETUP OUTPUT FILE
            //try
            //{
            //    StrmWtr_Main = new StreamWriter(folderFromFileName(OpnFlDlg_out.FileName), false);
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Unable to open file " + ex.Message);
            //    return;
            //}

            //foreach (XmlNode node in xmlRIDE.DocumentElement.ChildNodes)
            //    if (node.Name == "includedInsuredMemberIdentifier")
            //    {
            //        foreach (XmlNode subnode in node.ChildNodes)
            //            if (subnode.Name == "estimatedRIPayment")
            //                totalMoney += decimal.Parse(subnode.InnerText);
            //    }
            textBox1.Text = "";
            string strClaimID = "";
            string strServiceCode = "";
            string strBillTypeCode = "";
            string strReasonCode = "";
            bool bRejected = false;
            int count = 0;
            string strAllBillTypes = "";
            System.Collections.ArrayList alServiceCodes = new System.Collections.ArrayList();
            System.Collections.ArrayList alCounts = new System.Collections.ArrayList();
            StreamWriter sw = new StreamWriter("C:\\Users\\Pat\\Documents\\serviceCodes.txt");

            string allINVALIDscs = ",9350,0001F,0001M,0002M,0003M,0004M,0005F,0006M,0007M,0008M,0012F,0014F,0015F,0042T,0058T,0059T,0085T,0103T,0106T,0107T,0108T,0109T,0110T,0111T,0126T,0159T,0169T,0174T,0175T,0178T,0179T,0180T,0181T,0190T,1966,0197T,0198T,0199T,0205T,0206T,0223T,0224T,0225T,0226T,0227T,0232T,0233T,0239T,0240T,0241T,0243T,0244T,0255T,0272T,0273T,0286T,0287T,0289T,0291T,0292T,0295T,0296T,0297T,0298T,0305T,0306T,0311T,0326T,0327T,0328T,0329T,0330T,0331T,0332T,0333T,0337T,0341T,0346T,0347T,0348T,0349T,0350T,0351T,0352T,0353T,0354T,0355T,0356T,0357T,0358T,0359T,0360T,0361T,0362T,0363T,0364T,0365T,0366T,0367T,0368T,0369T,0370T,0371T,0372T,0373T,0374T,0375T,0376T,0377T,0378T,0379T,0380T,0500F,0501F,0502F,0503F,0505F,0507F,0509F,0513F,0514F,0516F,0517F,0518F,0519F,0520F,0521F,0525F,0526F,0528F,0529F,0535F,0540F,0545F,0550F,0551F,0555F,0556F,0557F,0575F,0580F,0581F,0582F,0583F,0584F,1000F,1002F,1003F,1004F,1005F,1006F,1007F,1008F,1010F,1011F,1012F,1015F,1018F,1019F,1022F,1026F,1030F,1031F,1032F,1033F,1034F,1035F,1036F,1038F,1039F,1040F,1050F,1052F,1055F,1060F,1061F,1065F,1066F,1070F,1071F,1090F,1091F,1100F,1101F,1110F,1111F,1116F,1118F,1119F,1121F,1123F,1124F,1125F,1126F,1127F,1128F,1130F,1134F,1135F,1136F,1137F,1150F,1151F,1152F,1153F,1157F,1158F,1159F,1160F,1170F,1175F,1180F,1181F,1182F,1183F,1200F,1205F,1220F,1400F,1450F,1451F,1460F,1461F,1490F,1491F,1493F,1494F,1500F,1501F,1502F,1503F,1504F,1505F,2000F,2001F,2002F,2004F,2010F,2014F,2015F,2016F,2018F,2019F,2020F,2021F,2022F,2024F,2026F,2027F,2028F,2029F,2030F,2031F,2035F,2040F,2044F,2050F,20604,20606,2060F,20611,20983,21811,21812,21813,22510,22511,22512,22513,22514,22515,22858,27279,3006F,3008F,3011F,3014F,3015F,3016F,3017F,3018F,3019F,3020F,3021F,3022F,3023F,3025F,3027F,3028F,3035F,3037F,3038F,3040F,3042F,3044F,3045F,3046F,3048F,3049F,3050F,3055F,3056F,3060F,3061F,3062F,3066F,3072F,3073F,3074F,3075F,3077F,3078F,3079F,3080F,3082F,3083F,3084F,3085F,3088F,3089F,3090F,3091F,3092F,3093F,3095F,3096F,3100F,3110F,3111F,3112F,3115F,3117F,3118F,3119F,3120F,3125F,3126F,3130F,3132F,3140F,3141F,3142F,3150F,3155F,3160F,3170F,3200F,3210F,3215F,3216F,3218F,3220F,3230F,3250F,3260F,3265F,3266F,3267F,3268F,3269F,3270F,3271F,3272F,3273F,3274F,3278F,3279F,3280F,3281F,3284F,3285F,3288F,3290F,3291F,3292F,3293F,3294F,3300F,3301F,3315F,3316F,3317F,3318F,3319F,3320F,3321F,3322F,3323F,3324F,3325F,33270,33271,33272,33273,3328F,3330F,3331F,3340F,33418,33419,3341F,3342F,3343F,3344F,3345F,3350F,3351F,3352F,3353F,3354F,3370F,3372F,3374F,3376F,3378F,3380F,3382F,3384F,3386F,3388F,3390F,33946,33947,33948,33949,3394F,33951," +
"33952,33953,33954,33955,33956,33957,33958,33959,3395F,33962,33963,33964,33965,33966,33969,33984,33985,33986,33987,33988,33989,3450F,3451F,3452F,3455F,3470F,3471F,3472F,3475F,3476F,34839,3490F,3491F,3492F,3493F,3494F,3495F,3496F,3497F,3498F,3500F,3502F,3503F,3510F,3511F,3512F,3513F,3514F,3515F,3517F,3520F,3550F,3551F,3552F,3555F,3570F,3572F,3573F,36415,36416,3650F,3700F,3720F,37218,3725F,3750F,3751F,3752F,3753F,3754F,3755F,3756F,3757F,3758F,3759F,3760F,3761F,3762F,3763F,3775F,3776F,4000F,4001F,4003F,4004F,4005F,4008F,4010F,4011F,4012F,4013F,4014F,4015F,4016F,4017F,4018F,4019F,4025F,4030F,4033F,4035F,4037F,4040F,4041F,4042F,4043F,4044F,4045F,4046F,4047F,4048F,4049F,4050F,4051F,4052F,4053F,4054F,4055F,4056F,4058F,4060F,4062F,4063F,4064F,4065F,4066F,4067F,4069F,4070F,4073F,4075F,4077F,4079F,4084F,4086F,4090F,4095F,4100F,4110F,4115F,4120F,4124F,4130F,4131F,4132F,4133F,4134F,4135F,4136F,4140F,4142F,4144F,4145F,4148F,4149F,4150F,4151F,4153F,4155F,4157F,4158F,4159F,4163F,4164F,4165F,4167F,4168F,4169F,4171F,4172F,4174F,4175F,4176F,4177F,4178F,4179F,4180F,4181F,4182F,4185F,4186F,4187F,4188F,4189F,4190F,4191F,4192F,4193F,4194F,4195F,4196F,4200F,4201F,4210F,4220F,4221F,4230F,4240F,4242F,4245F,4248F,4250F,4255F,4256F,4260F,4261F,4265F,4266F,4267F,4268F,4269F,4270F,4271F,4274F,4276F,4279F,4280F,4290F,4293F,4300F,4301F,4305F,4306F,43180,4320F,4322F,4324F,4325F,4326F,4328F,4330F,4340F,4350F,4400F,44381,44384,44401,44402,44403,44404,44405,44406,44407,44408,4450F,44705,4470F,4480F,4481F,4500F,4510F,4525F,4526F,45346,45347,45349," +
",45350,45388,45389,45390,45393,45398,45399,4540F,4541F,4550F,4551F,4552F,4553F,4554F,4555F,4556F,4557F,4558F,4559F,4560F,4561F,4562F,4563F,46601,46607,47383,5005F,5010F,5015F,5020F,5050F,5060F,5062F,5100F,5200F,52441,52442,5250F,59812,59840,59841,59850,59851,59852,59855,59856,59857,59866,6005F,6010F,6015F,6020F,6030F,6040F,6045F,6070F,6080F,6090F,6100F,6101F,6102F,6110F,6150F,62302,62303,62304,62305,64486,64487,64488,64489,66179,66184,70010,70015,70030,70100,7010F,70110,70120,70130,70134,70140,70150,70160,70170,70190,70200,7020F,70210,70220,70240,70250,7025F,70260,70300,70310,70320,70328,70330,70332,70336,70350,70355,70360,70370,70371,70373,70380,70390,70450,70460,70470,70480,70481,70482,70486,70487,70488,70490,70491,70492,70496,70498,70540,70542,70543,70544,70545,70546,70547,70548,70549,70551,70552,70553,70554,70555,70557,70558,70559,71010,71015,71020,71021,71022,71023,71030,71034,71035,71100,71101,71110,71111,71120,71130,71250,71260,71270,71275,71550,71551,71552,71555,72010,72020,72040,72050,72052,72069,72070,72072,72074,72080,72090,72100,72110,72114,72120,72125,72126,72127,72128,72129,72130,72131,72132,72133,72141,72142,72146,72147,72148,72149,72156,72157,72158,72159,72170,72190,72191,72192,72193,72194,72195,72196,72197,72198,72200,72202,72220,72240,72255,72265,72270,72275,72285,72291,72292,72295,73000,73010,73020,73030,73040,73050,73060,73070,73080,73085,73090,73092,73100,73110,73115,73120,73130,73140,73200,73201,73202,73206,73218,73219,73220,73221,73222,73223,73225,73500,73510,73520,73525,73530,73540,73550," +
",73560,73562,73564,73565,73580,73590,73592,73600,73610,73615,73620,73630,73650,73660,73700,73701,73702,73706,73718,73719,73720,73721,73722,73723,73725,74000,74010,74020,74022,74150,74160,74170,74174,74175,74176,74177,74178,74181,74182,74183,74185,74190,74210,74220,74230,74235,74240,74241,74245,74246,74247,74249,74250,74251,74260,74261,74262,74263,74270,74280,74283,74290,74291,74300,74301,74305,74320,74327,74328,74329,74330,74340,74355,74360,74363,74400,74410,74415,74420,74425,74430,74440,74445,74450,74455,74470,74475,74480,74485,74710,74740,74742,74775,75557,75559,75561,75563,75565,75571,75572,75573,75574,75600,75605,75625,75630,75635,75658,75705,75710,75716,75726,75731,75733,75736,75741,75743,75746,75756,75774,75791,75801,75803,75805,75807,75809,75810,75820,75822,75825,75827,75831,75833,75840,75842,75860,75870,75872,75880,75885,75887,75889,75891,75893,75894,75896,75898,75901,75902,75945,75946,75952,75953,75954,75956,75957,75958,75959,75962,75964,75966,75968,75970,75978,75980,75982,75984,75989,76000,76001,76010,76080,76098,76100,76101,76102,76120,76125,76140,76376,76377,76380,76390,76496,76497,76498,76499,76506,76510,76511,76512,76513,76514,76516,76519,76529,76536,76604,76641,76642,76645,76700,76705,76770,76775,76776,76800,76801,76802,76805,76810,76811,76812,76813,76814,76815,76816,76817,76818,76819,76820,76821,76825,76826,76827,76828,76830,76831,76856,76857,76870,76872,76873,76881,76882,76885,76886,76930,76932,76936,76937,76940,76941,76942,76945,76946,76948,76950,76965,76970,76975,76977,76998,76999,77001,77002," +
",77003,77011,77012,77013,77014,77021,77022,77051,77052,77053,77054,77055,77056,77057,77058,77059,77061,77062,77063,77071,77072,77073,77074,77075,77076,77077,77078,77080,77081,77082,77084,77085,77086,77306,77307,77316,77317,77318,77385,77386,77387,78012,78013,78014,78015,78016,78018,78020,78070,78071,78072,78075,78099,78102,78103,78104,78110,78111,78120,78121,78122,78130,78135,78140,78185,78190,78191,78195,78199,78201,78202,78205,78206,78215,78216,78226,78227,78230,78231,78232,78258,78261,78262,78264,78267,78268,78270,78271,78272,78278,78282,78290,78291,78299,78300,78305,78306,78315,78320,78350,78351,78399,78414,78428,78445,78451,78452,78453,78454,78456,78457,78458,78459,78466,78468,78469,78472,78473,78481,78483,78491,78492,78494,78496,78499,78579,78580,78582,78597,78598,78599,78600,78601,78605,78606,78607,78608,78609,78610,78630,78635,78645,78647,78650,78660,78699,78700,78701,78707,78708,78709,78710,78725,78730,78740,78761,78799,78800,78801,78802,78803,78804,78805,78806,78807,78808,78811,78812,78813,78814,78815,78816,78999,80047,80048,80050,80051,80053,80055,80061,80069,80074,80076,80100,80101,80102,80103,80104,80150,80152,80154,80155,80156,80157,80158,80159,80160,80162,80163,80164,80165,80166,80168,80169,80170,80171,80172,80173,80174,80175,80176,80177,80178,80180,80182,80183,80184,80185,80186,80188,80190,80192,80194,80195,80196,80197,80198,80199,80200,80201,80202,80203,80299,80300,80301,80302,80303,80304,80320,80321,80322,80323,80324,80325,80326,80327,80328,80329,80330,80331,80332,80333,80334,80335,80336,80337," +
",80338,80339,80340,80341,80342,80343,80344,80345,80346,80347,80348,80349,80350,80351,80352,80353,80354,80355,80356,80357,80358,80359,80360,80361,80362,80363,80364,80365,80366,80367,80368,80369,80370,80371,80372,80373,80374,80375,80376,80377,80400,80402,80406,80408,80410,80412,80414,80415,80416,80417,80418,80420,80422,80424,80426,80428,80430,80432,80434,80435,80436,80438,80439,80440,80500,80502,81000,81001,81002,81003,81005,81007,81015,81020,81025,81050,81099,81161,81200,81201,81202,81203,81205,81206,81207,81208,81209,81210,81211,81212,81213,81214,81215,81216,81217,81220,81221,81222,81223,81224,81225,81226,81227,81228,81229,81235,81240,81241,81242,81243,81244,81245,81246,81250,81251,81252,81253,81254,81255,81256,81257,81260,81261,81262,81263,81264,81265,81266,81267,81268,81270,81275,81280,81281,81282,81287,81288,81290,81291,81292,81293,81294,81295,81296,81297,81298,81299,81300,81301,81302,81303,81304,81310,81313,81315,81316,81317,81318,81319,81321,81322,81323,81324,81325,81326,81330,81331,81332,81340,81341,81342,81350,81355,81370,81371,81372,81373,81374,81375,81376,81377,81378,81379,81380,81381,81382,81383,81400,81401,81402,81403,81404,81405,81406,81407,81408,81410,81411,81415,81416,81417,81420,81425,81426,81427,81430,81431,81435,81436,81440,81445,81450,81455,81460,81465,81470,81471,81479,81500,81503,81504,81506,81507,81508,81509,81510,81511,81512,81519,81599,82000,82003,82009,82010,82013,82016,82017,82024,82030,82040,82042,82043,82044,82045,82055,82075,82085,82088,82101,82103,82104,82105,82106,82107,82108,82120," +
",82127,82128,82131,82135,82136,82139,82140,82143,82145,82150,82154,82157,82160,82163,82164,82172,82175,82180,82190,82205,82232,82239,82240,82247,82248,82252,82261,82270,82271,82272,82274,82286,82300,82306,82308,82310,82330,82331,82340,82355,82360,82365,82370,82373,82374,82375,82376,82378,82379,82380,82382,82383,82384,82387,82390,82397,82415,82435,82436,82438,82441,82465,82480,82482,82485,82486,82487,82488,82489,82491,82492,82495,82507,82520,82523,82525,82528,82530,82533,82540,82541,82542,82543,82544,82550,82552,82553,82554,82565,82570,82575,82585,82595,82600,82607,82608,82610,82615,82626,82627,82633,82634,82638,82646,82649,82651,82652,82654,82656,82657,82658,82664,82666,82668,82670,82671,82672,82677,82679,82690,82693,82696,82705,82710,82715,82725,82726,82728,82731,82735,82742,82746,82747,82757,82759,82760,82775,82776,82777,82784,82785,82787,82800,82803,82805,82810,82820,82930,82938,82941,82943,82945,82946,82947,82948,82950,82951,82952,82953,82955,82960,82962,82963,82965,82975,82977,82978,82979,82980,82985,83001,83002,83003,83006,83008,83009,83010,83012,83013,83014,83015,83018,83020,83021,83026,83030,83033,83036,83037,83045,83050,83051,83055,83060,83065,83068,83069,83070,83071,83080,83088,83090,83150,83491,83497,83498,83499,83500,83505,83516,83518,83519,83520,83525,83527,83528,83540,83550,83570,83582,83586,83593,83605,83615,83625,83630,83631,83632,83633,83634,83655,83661,83662,83663,83664,83670,83690,83695,83698,83700,83701,83704,83718,83719,83721,83727,83735,83775,83785,83788,83789,83805,83825,83835,83840,83857," +
",83858,83861,83864,83866,83872,83873,83874,83876,83880,83883,83885,83887,83915,83916,83918,83919,83921,83925,83930,83935,83937,83945,83950,83951,83970,83986,83987,83992,83993,84022,84030,84035,84060,84061,84066,84075,84078,84080,84080,84081,84085,84087,84100,84105,84106,84110,84112,84119,84120,84126,84127,84132,84133,84134,84135,84138,84140,84143,84144,84145,84146,84150,84152,84153,84154,84155,84156,84157,84160,84163,84165,84166,84181,84182,84202,84203,84206,84207,84210,84220,84228,84233,84234,84235,84238,84244,84252,84255,84260,84270,84275,84285,84295,84300,84302,84305,84307,84311,84315,84375,84376,84377,84378,84379,84392,84402,84403,84425,84430,84431,84432,84436,84437,84439,84442,84443,84445,84446,84449,84450,84460,84466,84478,84479,84480,84481,84482,84484,84485,84488,84490,84510,84512,84520,84525,84540,84545,84550,84560,84577,84578,84580,84583,84585,84586,84588,84590,84591,84597,84600,84620,84630,84681,84702,84703,84704,84830,84999,85002,85004,85007,85008,85009,85013,85014,85018,85025,85027,85032,85041,85044,85045,85046,85048,85049,85055,85060,85097,85130,85170,85175,85210,85220,85230,85240,85244,85245,85246,85247,85250,85260,85270,85280,85290,85291,85292,85293,85300,85301,85302,85303,85305,85306,85307,85335,85337,85345,85347,85348,85360,85362,85366,85370,85378,85379,85380,85384,85385,85390,85396,85397,85400,85410,85415,85420,85421,85441,85445,85460,85461,85475,85520,85525,85530,85536,85540,85547,85549,85555,85557,85576,85597,85598,85610,85611,85612,85613,85635,85651,85652,85660,85670,85675,85705,85730,85732," +
",85810,85999,86000,86001,86003,86005,86021,86022,86023,86038,86039,86060,86063,86077,86078,86079,86140,86141,86146,86147,86148,86152,86153,86155,86156,86157,86160,86161,86162,86171,86185,86200,86215,86225,86226,86235,86243,86255,86256,86277,86280,86294,86300,86301,86304,86305,86308,86309,86310,86316,86317,86318,86320,86325,86327,86329,86331,86332,86334,86335,86336,86337,86340,86341,86343,86344,86352,86353,86355,86356,86357,86359,86360,86361,86367,86376,86378,86382,86384,86386,86403,86406,86430,86431,86480,86481,86485,86486,86490,86510,86580,86590,86592,86593,86602,86603,86606,86609,86611,86612,86615,86617,86618,86619,86622,86625,86628,86631,86632,86635,86638,86641,86644,86645,86648,86651,86652,86653,86654,86658,86663,86664,86665,86666,86668,86671,86674,86677,86682,86684,86687,86688,86689,86692,86694,86695,86696,86698,86701,86702,86703,86704,86705,86706,86707,86708,86709,86710,86711,86713,86717,86720,86723,86727,86729,86732,86735,86738,86741,86744,86747,86750,86753,86756,86757,86759,86762,86765,86768,86771,86774,86777,86778,86780,86784,86787,86788,86789,86790,86793,86800,86803,86804,86805,86806,86807,86808,86812,86813,86816,86817,86821,86822,86825,86826,86828,86829,86830,86831,86832,86833,86834,86835,86849,86850,86860,86870,86880,86885,86886,86890,86891,86900,86901,86902,86904,86905,86906,86910,86911,86920,86921,86922,86923,86927,86930,86931,86932,86940,86941,86945,86950,86960,86965,86970,86971,86972,86975,86976,86977,86978,86985,86999,87001,87003,87015,87040,87045,87046,87070,87071,87073,87075,87076,87077,87081," +
",87084,87086,87088,87101,87102,87103,87106,87107,87109,87110,87116,87118,87140,87143,87147,87149,87150,87152,87153,87158,87164,87166,87168,87169,87172,87176,87177,87181,87184,87185,87186,87187,87188,87190,87197,87205,87206,87207,87209,87210,87220,87230,87250,87252,87253,87254,87255,87260,87265,87267,87269,87270,87271,87272,87273,87274,87275,87276,87277,87278,87279,87280,87281,87283,87285,87290,87299,87300,87301,87305,87320,87324,87327,87328,87329,87332,87335,87336,87337,87338,87339,87340,87341,87350,87380,87385,87389,87390,87391,87400,87420,87425,87427,87430,87449,87450,87451,87470,87471,87472,87475,87476,87477,87480,87481,87482,87485,87486,87487,87490,87491,87492,87493,87495,87496,87497,87498,87500,87501,87502,87503,87505,87506,87507,87510,87511,87512,87515,87516,87517,87520,87521,87522,87525,87526,87527,87528,87529,87530,87531,87532,87533,87534,87535,87536,87537,87538,87539,87540,87541,87542,87550,87551,87552,87555,87556,87557,87560,87561,87562,87580,87581,87582,87590,87591,87592,87620,87621,87622,87623,87624,87625,87631,87632,87633,87640,87641,87650,87651,87652,87653,87660,87661,87797,87798,87799,87800,87801,87802,87803,87804,87806,87807,87808,87809,87810,87850,87880,87899,87900,87901,87902,87903,87904,87905,87906,87910,87912,87999,88000,88005,88007,88012,88014,88016,88020,88025,88027,88028,88029,88036,88037,88040,88045,88099,88104,88106,88108,88112,88120,88121,88125,88130,88140,88141,88142,88143,88147,88148,88150,88152,88153,88154,88155,88160,88161,88162,88164,88165,88166,88167,88172,88173,88174,88175,88177," +
",88182,88184,88185,88187,88188,88189,88199,88230,88233,88235,88237,88239,88240,88241,88245,88248,88249,88261,88262,88263,88264,88267,88269,88271,88272,88273,88274,88275,88280,88283,88285,88289,88291,88299,88300,88302,88304,88305,88307,88309,88311,88312,88313,88314,88319,88321,88323,88325,88329,88331,88332,88333,88334,88341,88342,88343,88344,88346,88347,88348,88349,88355,88356,88358,88360,88361,88362,88363,88364,88365,88366,88367,88368,88369,88371,88372,88373,88374,88375,88377,88380,88381,88387,88388,88399,88720,88738,88740,88741,88749,89049,89050,89051,89055,89060,89125,89160,89190,89220,89230,89240,89250,89251,89253,89254,89255,89257,89258,89259,89260,89261,89264,89268,89272,89280,89281,89290,89291,89300,89310,89320,89321,89322,89325,89329,89330,89331,89335,89337,89342,89343,89344,89346,89352,89353,89354,89356,89398,9001F,9002F,9003F,9004F,9005F,9006F,9007F,90281,90283,90284,90287,90288,90291,90296,90371,90375,90376,90378,90384,90385,90386,90389,90393,90396,90399,90460,90461,90471,90472,90473,90474,90476,90477,90581,90585,90586,90630,90632,90633,90634,90636,90644,90645,90646,90647,90648,90649,90650,90651,90653,90654,90655,90656,90657,90658,90660,90661,90662,90664,90666,90667,90668,90669,90670,90672,90673,90675,90676,90680,90681,90685,90686,90687,90688,90690,90691,90692,90693,90696,90698,90700,90702,90703,90704,90705,90706,90707,90708,90710,90712,90713,90714,90715,90716,90717,90719,90720,90721,90723,90725,90727,90732,90733,90734,90735,90736,90738,90739,90740,90743,90744,90746,90747,90748,90749,90785,90882,90885," +
",90887,90889,90899,90901,90911,91010,91013,91020,91022,91030,91034,91035,91037,91038,91040,91065,91110,91111,91112,91117,91120,91122,91132,91133,91200,91299,92015,92020,92025,92060,92065,92071,92072,92081,92082,92083,92100,92132,92133,92134,92136,92140,92145,92230,92235,92240,92250,92260,92265,92270,92275,92283,92284,92285,92286,92287,92310,92311,92312,92313,92314,92315,92316,92317,92325,92326,92340,92341,92342,92352,92353,92354,92355,92358,92370,92371,92499,92511,92512,92516,92520,92531,92532,92533,92534,92540,92541,92542,92543,92544,92545,92546,92547,92548,92596,92597,92610,92611,92612,92613,92614,92615,92616,92617,93000,93005,93010,93015,93016,93017,93018,93024,93025,93040,93041,93042,93224,93225,93226,93227,93228,93229,93260,93261,93268,93270,93271,93272,93278,93279,93280,93281,93282,93283,93284,93285,93286,93287,93288,93289,93290,93291,93292,93293,93294,93295,93296,93297,93298,93299,93355,93561,93562,93563,93564,93565,93566,93567,93568,93571,93572,93600,93602,93603,93609,93610,93612,93613,93615,93616,93618,93619,93620,93621,93622,93623,93624,93631,93640,93641,93642,93644,93650,93660,93662,93668,93701,93702,93724,93740,93745,93750,93770,93784,93786,93788,93790,93799,93880,93882,93886,93888,93890,93892,93893,93895,93922,93923,93924,93925,93926,93930,93931,93965,93970,93971,93975,93976,93978,93979,93980,93981,93982,93990,93998,94010,94011,94012,94013,94014,94015,94016,94060,94070,94150,94200,94250,94375,94400,94450,94452,94453,94621,94680,94681,94690,94726,94727,94728,94729,94750,94760,94761,94762,94770,94772," +
",94774,94775,94776,94777,94780,94781,94799,95004,95012,95017,95018,95024,95027,95028,95044,95052,95056,95060,95065,95070,95071,95076,95079,95250,95251,95782,95783,95800,95801,95803,95805,95806,95807,95808,95810,95811,95812,95813,95816,95819,95822,95824,95827,95829,95830,95831,95832,95833,95834,95851,95852,95857,95860,95861,95863,95864,95865,95866,95867,95868,95869,95870,95872,95873,95874,95875,95885,95886,95887,95905,95907,95908,95909,95910,95911,95912,95913,95921,95922,95923,95924,95925,95926,95927,95928,95929,95930,95933,95937,95938,95939,95940,95941,95943,95950,95951,95953,95954,95955,95956,95957,95958,95961,95962,95965,95966,95967,95970,95971,95972,95973,95974,95975,95978,95979,95980,95981,95982,95990,95999,96000,96001,96002,96003,96127,96902,96904,97607,97608,97810,97811,97813,97814,98960,98961,98962,98966,98967,98968,98969,99000,99001,99002,99026,99027,99070,99071,99075,99078,99080,99082,99090,99091,99170,99172,99173,99174,99184,99188,99190,99191,99192,99199,99217,99238,99239,99315,99316,99339,99340,99360,99366,99367,99368,99374,99375,99377,99378,99379,99380,99401,99402,99403,99404,99406,99407,99408,99409,99411,99412,99420,99429,99441,99442,99443,99444,99446,99447,99448,99449,99450,99485,99486,99487,99488,99489,99490,99497,99498,99605,99606,99607,A0021,A0080,A0090,A0100,A0110,A0120,A0130,A0140,A0160,A0170,A0180,A0190,A0200,A0210,A0225,A0380,A0382,A0384,A0390,A0392,A0394,A0396,A0398,A0420,A0422,A0424,A0425,A0426,A0427,A0428,A0429,A0430,A0431,A0432,A0433,A0434,A0435,A0436,A0888,A0998,A0999,A4206,A4207,A4208," +
",A4209,A4210,A4211,A4212,A4213,A4215,A4216,A4217,A4218,A4220,A4221,A4222,A4223,A4230,A4231,A4232,A4233,A4234,A4235,A4236,A4244,A4245,A4246,A4247,A4248,A4250,A4252,A4253,A4255,A4256,A4257,A4258,A4259,A4261,A4262,A4263,A4264,A4265,A4266,A4267,A4268,A4269,A4270,A4280,A4281,A4282,A4283,A4284,A4285,A4286,A4290,A4300,A4301,A4305,A4306,A4310,A4311,A4312,A4313,A4314,A4315,A4316,A4320,A4321,A4322,A4326,A4327,A4328,A4330,A4331,A4332,A4333,A4334,A4335,A4336,A4338,A4340,A4344,A4346,A4349,A4351,A4352,A4353,A4354,A4355,A4356,A4357,A4358,A4360,A4361,A4362,A4363,A4364,A4366,A4367,A4368,A4369,A4371,A4372,A4373,A4375,A4376,A4377,A4378,A4379,A4380,A4381,A4382,A4383,A4384,A4385,A4387,A4388,A4389,A4390,A4391,A4392,A4393,A4394,A4395,A4396,A4397,A4398,A4399,A4400,A4402,A4404,A4405,A4406,A4407,A4408,A4409,A4410,A4411,A4412,A4413,A4414,A4415,A4416,A4417,A4418,A4419,A4420,A4421,A4422,A4423,A4424,A4425,A4426,A4427,A4428,A4429,A4430,A4431,A4432,A4433,A4434,A4435,A4450,A4452,A4455,A4456,A4458,A4459,A4461,A4463,A4465,A4466,A4470,A4480,A4481,A4483,A4490,A4495,A4500,A4510,A4520,A4550,A4554,A4555,A4556,A4557,A4558,A4559,A4561,A4562,A4565,A4566,A4570,A4575,A4580,A4590,A4595,A4600,A4601,A4602,A4604,A4605,A4606,A4608,A4611,A4612,A4613,A4614,A4615,A4616,A4617,A4618,A4619,A4620,A4623,A4624,A4625,A4626,A4627,A4628,A4629,A4630,A4633,A4634,A4635,A4636,A4637,A4638,A4639,A4640,A4641,A4642,A4648,A4649,A4650,A4651,A4652,A4653,A4657,A4660,A4663,A4670,A4671,A4672,A4673,A4674,A4680,A4690,A4706,A4707,A4708,A4709,A4714,A4719,A4720,A4721,A4722,A4723,A4724,A4725," +
",A4726,A4728,A4730,A4736,A4737,A4740,A4750,A4755,A4760,A4765,A4766,A4770,A4771,A4772,A4773,A4774,A4802,A4860,A4870,A4890,A4911,A4913,A4918,A4927,A4928,A4929,A4930,A4931,A4932,A5051,A5052,A5053,A5054,A5055,A5056,A5057,A5061,A5062,A5063,A5071,A5072,A5073,A5081,A5082,A5083,A5093,A5102,A5105,A5112,A5113,A5114,A5120,A5121,A5122,A5126,A5131,A5200,A5500,A5501,A5503,A5504,A5505,A5506,A5507,A5508,A5510,A5512,A5513,A6000,A6010,A6011,A6021,A6022,A6023,A6024,A6025,A6154,A6196,A6197,A6198,A6199,A6203,A6204,A6205,A6206,A6207,A6208,A6209,A6210,A6211,A6212,A6213,A6214,A6215,A6216,A6217,A6218,A6219,A6220,A6221,A6222,A6223,A6224,A6228,A6229,A6230,A6231,A6232,A6233,A6234,A6235,A6236,A6237,A6238,A6239,A6240,A6241,A6242,A6243,A6244,A6245,A6246,A6247,A6248,A6250,A6251,A6252,A6253,A6254,A6255,A6256,A6257,A6258,A6259,A6260,A6261,A6262,A6266,A6402,A6403,A6404,A6407,A6410,A6411,A6412,A6413,A6441,A6442,A6443,A6444,A6445,A6446,A6447,A6448,A6449,A6450,A6451,A6452,A6453,A6454,A6455,A6456,A6457,A6501,A6502,A6503,A6504,A6505,A6506,A6507,A6508,A6509,A6510,A6511,A6512,A6513,A6530,A6531,A6532,A6533,A6534,A6535,A6536,A6537,A6538,A6539,A6540,A6541,A6544,A6545,A6549,A6550,A7000,A7001,A7002,A7003,A7004,A7005,A7006,A7007,A7008,A7009,A7010,A7011,A7012,A7013,A7014,A7015,A7016,A7017,A7018,A7020,A7025,A7026,A7027,A7028,A7029,A7030,A7031,A7032,A7033,A7034,A7035,A7036,A7037,A7038,A7039,A7040,A7041,A7042,A7043,A7044,A7045,A7046,A7047,A7048,A7501,A7502,A7503,A7504,A7505,A7506,A7507,A7508,A7509,A7520,A7521,A7522,A7523,A7524,A7525,A7526,A7527,A8000,A8001,A8002," +
",A8003,A8004,A9150,A9152,A9153,A9155,A9180,A9270,A9272,A9273,A9274,A9275,A9276,A9277,A9278,A9279,A9280,A9281,A9282,A9283,A9284,A9300,A9500,A9501,A9502,A9503,A9504,A9505,A9507,A9508,A9509,A9510,A9512,A9516,A9517,A9520,A9521,A9524,A9526,A9527,A9528,A9529,A9530,A9531,A9532,A9536,A9537,A9538,A9539,A9540,A9541,A9542,A9543,A9544,A9545,A9546,A9547,A9548,A9550,A9551,A9552,A9553,A9554,A9555,A9556,A9557,A9558,A9559,A9560,A9561,A9562,A9563,A9564,A9566,A9567,A9568,A9569,A9570,A9571,A9572,A9575,A9576,A9577,A9578,A9579,A9580,A9581,A9582,A9583,A9584,A9585,A9586,A9599,A9600,A9604,A9606,A9698,A9699,A9700,A9900,A9901,A9999,B4034,B4035,B4036,B4081,B4082,B4083,B4087,B4088,B4100,B4102,B4103,B4104,B4149,B4150,B4152,B4153,B4154,B4155,B4157,B4158,B4159,B4160,B4161,B4162,B4164,B4168,B4172,B4176,B4178,B4180,B4185,B4189,B4193,B4197,B4199,B4216,B4220,B4222,B4224,B5000,B5100,B5200,B9000,B9002,B9004,B9006,B9998,B9999,C1204,C1300,C1713,C1714,C1715,C1716,C1717,C1719,C1721,C1722,C1724,C1725,C1726,C1727,C1728,C1729,C1730,C1731,C1732,C1733,C1749,C1750,C1751,C1752,C1753,C1754,C1755,C1756,C1757,C1758,C1759,C1760,C1762,C1763,C1764,C1765,C1766,C1767,C1768,C1769,C1770,C1771,C1772,C1773,C1776,C1777,C1778,C1779,C1780,C1781,C1782,C1783,C1784,C1785,C1786,C1787,C1788,C1789,C1813,C1814,C1815,C1816,C1817,C1818,C1819,C1820,C1821,C1830,C1840,C1841,C1874,C1875,C1876,C1877,C1878,C1879,C1880,C1881,C1882,C1883,C1884,C1885,C1886,C1887,C1888,C1891,C1892,C1893,C1894,C1895,C1896,C1897,C1898,C1899,C1900,C2614,C2615,C2616,C2617,C2618,C2619,C2620,C2621,C2622,C2624,C2625," +
",C2626,C2627,C2628,C2629,C2630,C2631,C2634,C2635,C2636,C2637,C2638,C2639,C2640,C2641,C2642,C2643,C2644,C2698,C2699,C5271,C5272,C5273,C5274,C5275,C5276,C5277,C5278,C8900,C8901,C8902,C8903,C8904,C8905,C8906,C8907,C8908,C8909,C8910,C8911,C8912,C8913,C8914,C8918,C8919,C8920,C8921,C8922,C8923,C8924,C8925,C8926,C8927,C8928,C8929,C8930,C8931,C8932,C8933,C8934,C8935,C8936,C8957,C9021,C9022,C9023,C9025,C9026,C9027,C9113,C9121,C9130,C9131,C9132,C9133,C9134,C9135,C9136,C9248,C9250,C9254,C9255,C9256,C9257,C9258,C9259,C9260,C9261,C9262,C9263,C9264,C9265,C9266,C9267,C9268,C9269,C9270,C9271,C9272,C9273,C9274,C9275,C9276,C9277,C9278,C9279,C9280,C9281,C9282,C9283,C9284,C9285,C9286,C9287,C9288,C9289,C9290,C9291,C9292,C9293,C9294,C9295,C9296,C9297,C9298,C9349,C9352,C9353,C9354,C9355,C9356,C9358,C9359,C9360,C9361,C9362,C9363,C9364,C9365,C9366,C9367,C9368,C9369,C9399,C9406,C9441,C9442,C9443,C9444,C9446,C9447,C9497,C9600,C9601,C9602,C9603,C9604,C9605,C9606,C9607,C9608,C9716,C9724,C9725,C9726,C9727,C9728,C9729,C9730,C9731,C9732,C9733,C9734,C9735,C9736,C9737,C9739,C9740,C9741,C9742,C9800,C9801,C9802,C9898,C9899,E0100,E0105,E0110,E0111,E0112,E0113,E0114,E0116,E0117,E0118,E0130,E0135,E0140,E0141,E0143,E0144,E0147,E0148,E0149,E0153,E0154,E0155,E0156,E0157,E0158,E0159,E0160,E0161,E0162,E0163,E0165,E0167,E0168,E0170,E0171,E0172,E0175,E0181,E0182,E0184,E0185,E0186,E0187,E0188,E0189,E0190,E0191,E0193,E0194,E0196,E0197,E0198,E0199,E0200,E0202,E0203,E0205,E0210,E0215,E0217,E0218,E0220,E0221,E0225,E0230,E0231,E0232,E0235,E0236,E0238,E0239,E0240," +
",E0241,E0242,E0243,E0244,E0245,E0246,E0247,E0248,E0249,E0250,E0251,E0255,E0256,E0260,E0261,E0265,E0266,E0270,E0271,E0272,E0273,E0274,E0275,E0276,E0277,E0280,E0290,E0291,E0292,E0293,E0294,E0295,E0296,E0297,E0300,E0301,E0302,E0303,E0304,E0305,E0310,E0315,E0316,E0325,E0326,E0328,E0329,E0350,E0352,E0370,E0371,E0372,E0373,E0424,E0425,E0430,E0431,E0433,E0434,E0435,E0439,E0440,E0441,E0442,E0443,E0444,E0445,E0446,E0450,E0455,E0457,E0459,E0460,E0461,E0462,E0463,E0464,E0470,E0471,E0472,E0480,E0481,E0482,E0483,E0484,E0485,E0486,E0487,E0500,E0550,E0555,E0560,E0561,E0562,E0565,E0570,E0571,E0572,E0574,E0575,E0580,E0585,E0600,E0601,E0602,E0603,E0604,E0605,E0606,E0607,E0610,E0615,E0616,E0617,E0618,E0619,E0620,E0621,E0625,E0627,E0628,E0629,E0630,E0635,E0636,E0637,E0638,E0639,E0640,E0641,E0642,E0650,E0651,E0652,E0655,E0656,E0657,E0660,E0665,E0666,E0667,E0668,E0669,E0670,E0671,E0672,E0673,E0675,E0676,E0691,E0692,E0693,E0694,E0700,E0705,E0710,E0720,E0730,E0731,E0740,E0744,E0745,E0746,E0747,E0748,E0749,E0755,E0760,E0761,E0762,E0764,E0765,E0766,E0769,E0770,E0776,E0779,E0780,E0781,E0782,E0783,E0784,E0785,E0786,E0791,E0830,E0840,E0849,E0850,E0855,E0856,E0860,E0870,E0880,E0890,E0900,E0910,E0911,E0912,E0920,E0930,E0935,E0936,E0940,E0941,E0942,E0944,E0945,E0946,E0947,E0948,E0950,E0951,E0952,E0955,E0956,E0957,E0958,E0959,E0960,E0961,E0966,E0967,E0968,E0969,E0970,E0971,E0973,E0974,E0978,E0980,E0981,E0982,E0983,E0984,E0985,E0986,E0988,E0990,E0992,E0994,E0995,E1002,E1003,E1004,E1005,E1006,E1007,E1008,E1009,E1010,E1011,E1014,E1015,E1016,E1017," +
",E1018,E1020,E1028,E1029,E1030,E1031,E1035,E1036,E1037,E1038,E1039,E1050,E1060,E1070,E1083,E1084,E1085,E1086,E1087,E1088,E1089,E1090,E1092,E1093,E1100,E1110,E1130,E1140,E1150,E1160,E1161,E1170,E1171,E1172,E1180,E1190,E1195,E1200,E1220,E1221,E1222,E1223,E1224,E1225,E1226,E1227,E1228,E1229,E1230,E1231,E1232,E1233,E1234,E1235,E1236,E1237,E1238,E1239,E1240,E1250,E1260,E1270,E1280,E1285,E1290,E1295,E1296,E1297,E1298,E1300,E1310,E1352,E1353,E1354,E1355,E1356,E1357,E1358,E1372,E1390,E1391,E1392,E1399,E1405,E1406,E1500,E1510,E1520,E1530,E1540,E1550,E1560,E1570,E1575,E1580,E1590,E1592,E1594,E1600,E1610,E1615,E1620,E1625,E1630,E1632,E1634,E1635,E1636,E1637,E1639,E1699,E1700,E1701,E1702,E1800,E1801,E1802,E1805,E1806,E1810,E1811,E1812,E1815,E1816,E1818,E1820,E1821,E1825,E1830,E1831,E1840,E1841,E1902,E2000,E2100,E2101,E2120,E2201,E2202,E2203,E2204,E2205,E2206,E2207,E2208,E2209,E2210,E2211,E2212,E2213,E2214,E2215,E2216,E2217,E2218,E2219,E2220,E2221,E2222,E2224,E2225,E2226,E2227,E2228,E2230,E2231,E2291,E2292,E2293,E2294,E2295,E2300,E2301,E2310,E2311,E2312,E2313,E2321,E2322,E2323,E2324,E2325,E2326,E2327,E2328,E2329,E2330,E2331,E2340,E2341,E2342,E2343,E2351,E2358,E2359,E2360,E2361,E2362,E2363,E2364,E2365,E2366,E2367,E2368,E2369,E2370,E2371,E2372,E2373,E2374,E2375,E2376,E2377,E2378,E2381,E2382,E2383,E2384,E2385,E2386,E2387,E2388,E2389,E2390,E2391,E2392,E2394,E2395,E2396,E2397,E2402,E2500,E2502,E2504,E2506,E2508,E2510,E2511,E2512,E2599,E2601,E2602,E2603,E2604,E2605,E2606,E2607,E2608,E2609,E2610,E2611,E2612,E2613,E2614,E2615,E2616," +
",E2617,E2619,E2620,E2621,E2622,E2623,E2624,E2625,E2626,E2627,E2628,E2629,E2630,E2631,E2632,E2633,E8000,E8001,E8002,G0008,G0009,G0010,G0027,G0101,G0102,G0103,G0104,G0105,G0106,G0108,G0109,G0117,G0118,G0120,G0121,G0122,G0123,G0124,G0127,G0128,G0130,G0141,G0143,G0144,G0145,G0147,G0148,G0154,G0156,G0157,G0158,G0162,G0163,G0164,G0166,G0168,G0175,G0176,G0177,G0179,G0180,G0181,G0182,G0202,G0204,G0206,G0219,G0235,G0237,G0238,G0239,G0252,G0255,G0270,G0271,G0275,G0276,G0277,G0278,G0279,G0281,G0282,G0283,G0288,G0290,G0291,G0293,G0294,G0295,G0302,G0303,G0304,G0305,G0306,G0307,G0328,G0329,G0333,G0337,G0364,G0365,G0372,G0389,G0390,G0396,G0397,G0398,G0399,G0400,G0403,G0404,G0405,G0410,G0411,G0416,G0417,G0418,G0419,G0420,G0421,G0422,G0423,G0424,G0430,G0431,G0432,G0433,G0434,G0435,G0436,G0437,G0442,G0444,G0445,G0446,G0451,G0452,G0453,G0454,G0461,G0462,G0464,G0466,G0467,G0468,G0469,G0470,G0471,G0472,G0473,G0908,G0909,G0910,G0911,G0912,G0913,G0914,G0915,G0916,G0917,G0918,G0919,G0920,G0921,G0922,G6001,G6002,G6003,G6004,G6005,G6006,G6007,G6008,G6009,G6010,G6011,G6012,G6013,G6014,G6015,G6016,G6017,G6018,G6019,G6020,G6021,G6022,G6023,G6024,G6025,G6027,G6028,G6030,G6031,G6032,G6034,G6035,G6036,G6037,G6038,G6039,G6040,G6041,G6042,G6043,G6044,G6045,G6046,G6047,G6048,G6049,G6050,G6051,G6052,G6053,G6054,G6055,G6056,G6057,G6058,G8006,G8007,G8008,G8009,G8010,G8011,G8012,G8013,G8014,G8015,G8016,G8017,G8018,G8019,G8020,G8021,G8022,G8023,G8024,G8025,G8026,G8027,G8028,G8029,G8030,G8031,G8032,G8033,G8034,G8035,G8036,G8037,G8038,G8039,G8040,G8041," +
",G8051,G8052,G8053,G8054,G8055,G8056,G8057,G8058,G8059,G8060,G8061,G8062,G8075,G8076,G8077,G8078,G8079,G8080,G8081,G8082,G8085,G8093,G8094,G8099,G8100,G8103,G8104,G8106,G8107,G8108,G8109,G8110,G8111,G8112,G8113,G8114,G8115,G8116,G8117,G8126,G8127,G8128,G8129,G8130,G8131,G8152,G8153,G8154,G8155,G8156,G8157,G8159,G8162,G8164,G8165,G8166,G8167,G8170,G8171,G8172,G8182,G8183,G8184,G8185,G8186,G8193,G8196,G8200,G8204,G8209,G8214,G8217,G8219,G8220,G8221,G8223,G8226,G8231,G8234,G8238,G8240,G8243,G8246,G8248,G8251,G8254,G8257,G8260,G8263,G8266,G8268,G8271,G8274,G8276,G8279,G8282,G8285,G8289,G8293,G8296,G8298,G8299,G8302,G8303,G8304,G8305,G8306,G8307,G8308,G8310,G8314,G8318,G8322,G8326,G8330,G8334,G8338,G8341,G8345,G8351,G8354,G8357,G8360,G8362,G8365,G8367,G8370,G8371,G8372,G8373,G8374,G8375,G8376,G8377,G8378,G8379,G8380,G8381,G8382,G8383,G8384,G8385,G8386,G8387,G8388,G8389,G8390,G8391,G8395,G8396,G8397,G8398,G8399,G8400,G8401,G8402,G8403,G8404,G8405,G8406,G8407,G8408,G8409,G8410,G8415,G8416,G8417,G8418,G8419,G8420,G8421,G8422,G8423,G8424,G8425,G8426,G8427,G8428,G8429,G8430,G8431,G8432,G8433,G8434,G8435,G8436,G8437,G8438,G8439,G8440,G8441,G8442,G8443,G8445,G8446,G8447,G8448,G8449,G8450,G8451,G8452,G8453,G8454,G8455,G8456,G8457,G8458,G8459,G8460,G8461,G8462,G8463,G8464,G8465,G8466,G8467,G8468,G8469,G8470,G8471,G8472,G8473,G8474,G8475,G8476,G8477,G8478,G8479,G8480,G8481,G8482,G8483,G8484,G8485,G8486,G8487,G8488,G8489,G8490,G8491,G8492,G8493,G8494,G8495,G8496,G8497,G8498,G8499,G8500,G8501,G8502,G8506,G8507,G8508,G8509,G8510," +
",G8511,G8518,G8519,G8520,G8524,G8525,G8526,G8530,G8531,G8532,G8534,G8535,G8536,G8537,G8538,G8539,G8540,G8541,G8542,G8543,G8544,G8545,G8546,G8547,G8548,G8549,G8550,G8551,G8552,G8553,G8556,G8557,G8558,G8559,G8560,G8561,G8562,G8563,G8564,G8565,G8566,G8567,G8568,G8569,G8570,G8571,G8572,G8573,G8574,G8575,G8576,G8577,G8578,G8579,G8580,G8581,G8582,G8583,G8584,G8585,G8586,G8587,G8588,G8589,G8590,G8591,G8592,G8593,G8594,G8595,G8596,G8597,G8598,G8599,G8600,G8601,G8602,G8603,G8604,G8605,G8606,G8607,G8608,G8609,G8610,G8611,G8612,G8613,G8614,G8615,G8616,G8617,G8618,G8619,G8620,G8621,G8622,G8623,G8624,G8625,G8626,G8627,G8628,G8629,G8630,G8631,G8632,G8633,G8634,G8635,G8636,G8637,G8638,G8639,G8640,G8641,G8642,G8643,G8644,G8645,G8646,G8647,G8648,G8649,G8650,G8651,G8652,G8653,G8654,G8655,G8656,G8657,G8658,G8659,G8660,G8661,G8662,G8663,G8664,G8665,G8666,G8667,G8668,G8669,G8670,G8671,G8672,G8673,G8674,G8675,G8676,G8677,G8678,G8679,G8680,G8681,G8682,G8683,G8684,G8685,G8686,G8687,G8688,G8689,G8690,G8691,G8692,G8693,G8694,G8695,G8696,G8697,G8698,G8699,G8700,G8701,G8702,G8703,G8704,G8705,G8706,G8707,G8708,G8709,G8710,G8711,G8712,G8713,G8714,G8715,G8716,G8717,G8718,G8720,G8721,G8722,G8723,G8724,G8725,G8726,G8727,G8728,G8730,G8731,G8732,G8733,G8734,G8735,G8736,G8737,G8738,G8739,G8740,G8741,G8742,G8743,G8744,G8745,G8746,G8747,G8748,G8749,G8750,G8751,G8752,G8753,G8754,G8755,G8756,G8757,G8758,G8759,G8760,G8761,G8762,G8763,G8764,G8765,G8767,G8768,G8769,G8770,G8771,G8772,G8773,G8774,G8775,G8776,G8777,G8778,G8779,G8780,G8781,G8782,G8783,G8784," +
",G8785,G8786,G8787,G8788,G8789,G8790,G8791,G8792,G8793,G8794,G8795,G8796,G8797,G8798,G8799,G8800,G8801,G8802,G8803,G8805,G8806,G8807,G8808,G8809,G8810,G8811,G8812,G8813,G8814,G8815,G8816,G8817,G8818,G8819,G8820,G8821,G8822,G8823,G8824,G8825,G8826,G8827,G8828,G8829,G8830,G8831,G8832,G8833,G8834,G8835,G8836,G8837,G8838,G8839,G8840,G8841,G8842,G8843,G8844,G8845,G8846,G8847,G8848,G8849,G8850,G8851,G8852,G8853,G8854,G8855,G8856,G8857,G8858,G8859,G8860,G8861,G8862,G8863,G8864,G8865,G8866,G8867,G8868,G8869,G8870,G8871,G8872,G8873,G8874,G8875,G8876,G8877,G8878,G8879,G8880,G8881,G8882,G8883,G8884,G8885,G8886,G8887,G8888,G8889,G8890,G8891,G8892,G8893,G8894,G8895,G8896,G8897,G8898,G8899,G8900,G8901,G8902,G8903,G8904,G8905,G8906,G8907,G8908,G8909,G8910,G8911,G8912,G8913,G8914,G8915,G8916,G8917,G8918,G8919,G8920,G8921,G8922,G8923,G8924,G8925,G8926,G8927,G8928,G8929,G8930,G8931,G8932,G8933,G8934,G8935,G8936,G8937,G8938,G8939,G8940,G8941,G8942,G8943,G8944,G8945,G8946,G8947,G8948,G8949,G8950,G8951,G8952,G8953,G8954,G8955,G8956,G8957,G8958,G8959,G8960,G8961,G8962,G8963,G8964,G8965,G8966,G8967,G8968,G8969,G8970,G8971,G8972,G8973,G8974,G8975,G8976,G8977,G8978,G8979,G8980,G8981,G8982,G8983,G8952,G8953,G8954,G8955,G8956,G8957,G8958,G8959,G8960,G8961,G8962,G8963,G8964,G8965,G8966,G8967,G8968,G8969,G8970,G8971,G8972,G8973,G8974,G8975,G8976,G8977,G8978,G8979,G8980,G8981,G8982,G8983,G8984,G8985,G8986,G8987,G8988,G8989,G8990,G8991,G8992,G8993,G8994,G8995,G8996,G8997,G8998,G8999,G9001,G9002,G9003,G9004,G9005,G9006,G9007,G9008,G9009,G9010," +
",G9011,G9012,G9013,G9014,G9016,G9017,G9018,G9019,G9020,G9033,G9034,G9035,G9036,G9041,G9042,G9043,G9044,G9050,G9051,G9052,G9053,G9054,G9055,G9056,G9057,G9058,G9059,G9060,G9061,G9062,G9063,G9064,G9065,G9066,G9067,G9068,G9069,G9070,G9071,G9072,G9073,G9074,G9075,G9077,G9078,G9079,G9080,G9083,G9084,G9085,G9086,G9087,G9088,G9089,G9090,G9091,G9092,G9093,G9094,G9095,G9096,G9097,G9098,G9099,G9100,G9101,G9102,G9103,G9104,G9105,G9106,G9107,G9108,G9109,G9110,G9111,G9112,G9113,G9114,G9115,G9116,G9117,G9123,G9124,G9125,G9126,G9128,G9129,G9130,G9131,G9132,G9133,G9134,G9135,G9136,G9137,G9138,G9139,G9140,G9141,G9142,G9143,G9147,G9148,G9149,G9150,G9151,G9152,G9153,G9156,G9157,G9158,G9159,G9160,G9161,G9162,G9163,G9164,G9165,G9166,G9167,G9168,G9169,G9170,G9171,G9172,G9173,G9174,G9175,G9176,G9186,G9187,G9188,G9189,G9190,G9191,G9192,G9193,G9194,G9195,G9196,G9197,G9198,G9199,G9200,G9201,G9202,G9203,G9204,G9205,G9206,G9207,G9208,G9209,G9210,G9211,G9212,G9213,G9214,G9215,G9216,G9217,G9218,G9219,G9220,G9221,G9222,G9223,G9224,G9225,G9226,G9227,G9228,G9229,G9230,G9231,G9232,G9233,G9234,G9235,G9236,G9237,G9238,G9239,G9240,G9241,G9242,G9243,G9244,G9245,G9246,G9247,G9248,G9249,G9250,G9251,G9252,G9253,G9254,G9255,G9256,G9257,G9258,G9259,G9260,G9261,G9262,G9263,G9264,G9265,G9266,G9267,G9268,G9269,G9270,G9271,G9272,G9273,G9274,G9275,G9276,G9277,G9278,G9279,G9280,G9281,G9282,G9283,G9284,G9285,G9286,G9287,G9288,G9289,G9290,G9291,G9292,G9293,G9294,G9295,G9296,G9297,G9298,G9299,G9300,G9301,G9302,G9303,G9304,G9305,G9306,G9307,G9308,G9309,G9310,G9311," +
",G9312,G9313,G9314,G9315,G9316,G9317,G9318,G9319,G9320,G9321,G9322,G9323,G9324,G9325,G9326,G9327,G9328,G9329,G9340,G9341,G9342,G9343,G9344,G9345,G9346,G9347,G9348,G9349,G9350,G9351,G9352,G9353,G9354,G9355,G9356,G9357,G9358,G9359,G9360,G9361,G9362,G9363,G9364,G9365,G9366,G9367,G9368,G9369,G9370,G9376,G9377,G9378,G9379,G9380,G9381,G9382,G9383,G9384,G9385,G9386,G9389,G9390,G9391,G9392,G9393,G9394,G9395,G9396,G9399,G9400,G9401,G9402,G9403,G9404,G9405,G9406,G9407,G9408,G9409,G9410,G9411,G9412,G9413,G9414,G9415,G9416,G9417,G9418,G9419,G9420,G9421,G9422,G9423,G9424,G9425,G9426,G9427,G9428,G9429,G9430,G9431,G9432,G9433,G9434,G9435,G9436,G9437,G9438,G9439,G9440,G9441,G9442,G9443,G9448,G9449,G9450,G9451,G9452,G9453,G9454,G9455,G9456,G9457,G9458,G9459,G9460,G9463,G9464,G9465,G9466,G9467,G9468,G9469,G9470,G9471,G9472,H0001,H0002,H0003,H0004,H0005,H0006,H0017,H0018,H0019,H0021,H0022,H0023,H0024,H0025,H0026,H0027,H0028,H0029,H0030,H0031,H0032,H0033,H0034,H0035,H0036,H0037,H0038,H0039,H0040,H0041,H0042,H0043,H0044,H0045,H0046,H0047,H0048,H0049,H1010,H1011,H2000,H2001,H2010,H2011,H2012,H2013,H2014,H2015,H2016,H2017,H2018,H2019,H2020,H2021,H2022,H2023,H2024,H2025,H2026,H2027,H2028,H2029,H2030,H2031,H2032,H2034,H2035,H2036,H2037,J0120,J0128,J0129,J0130,J0131,J0132,J0133,J0135,J0150,J0151,J0152,J0153,J0170,J0171,J0178,J0180,J0190,J0200,J0205,J0207,J0210,J0215,J0220,J0221,J0256,J0257,J0270,J0275,J0278,J0280,J0282,J0285,J0287,J0288,J0289,J0290,J0295,J0300,J0330,J0348,J0350,J0360,J0364,J0365,J0380,J0390,J0395,J0400,J0401,J0456,J0461," +
",J0470,J0475,J0476,J0480,J0485,J0490,J0500,J0515,J0520,J0558,J0559,J0560,J0561,J0570,J0571,J0572,J0573,J0574,J0575,J0580,J0583,J0585,J0586,J0587,J0588,J0592,J0594,J0595,J0597,J0598,J0600,J0610,J0620,J0630,J0636,J0637,J0638,J0640,J0641,J0670,J0690,J0692,J0694,J0696,J0697,J0698,J0702,J0704,J0706,J0710,J0712,J0713,J0715,J0716,J0717,J0718,J0720,J0725,J0735,J0740,J0743,J0744,J0745,J0760,J0770,J0775,J0780,J0795,J0800,J0833,J0834,J0840,J0850,J0878,J0881,J0882,J0885,J0886,J0887,J0888,J0890,J0894,J0895,J0897,J0900,J0945,J0970,J1000,J1020,J1030,J1040,J1050,J1051,J1055,J1056,J1060,J1070,J1071,J1080,J1094,J1100,J1110,J1120,J1160,J1162,J1165,J1170,J1180,J1190,J1200,J1205,J1212,J1230,J1240,J1245,J1250,J1260,J1265,J1267,J1270,J1290,J1300,J1320,J1322,J1324,J1325,J1327,J1330,J1335,J1364,J1380,J1390,J1410,J1430,J1435,J1436,J1438,J1439,J1440,J1441,J1442,J1446,J1450,J1451,J1452,J1453,J1455,J1457,J1458,J1459,J1460,J1470,J1480,J1490,J1500,J1510,J1520,J1530,J1540,J1550,J1556,J1557,J1559,J1560,J1561,J1562,J1566,J1568,J1569,J1570,J1571,J1572,J1573,J1580,J1590,J1595,J1599,J1600,J1602,J1610,J1620,J1626,J1630,J1631,J1640,J1642,J1644,J1645,J1650,J1652,J1655,J1670,J1675,J1680,J1700,J1710,J1720,J1725,J1730,J1740,J1741,J1742,J1743,J1744,J1745,J1750,J1756,J1785,J1786,J1790,J1800,J1810,J1815,J1817,J1825,J1826,J1830,J1835,J1840,J1850,J1885,J1890,J1930,J1931,J1940,J1945,J1950,J1953,J1955,J1956,J1960,J1980,J1990,J2001,J2010,J2020,J2060,J2150,J2170,J2175,J2180,J2185,J2210,J2212,J2248,J2250,J2260,J2265,J2270,J2271,J2274,J2275,J2278,J2280,J2300,J2310," +
",J2315,J2320,J2321,J2322,J2323,J2325,J2353,J2354,J2355,J2357,J2358,J2360,J2370,J2400,J2405,J2410,J2425,J2426,J2430,J2440,J2460,J2469,J2501,J2503,J2504,J2505,J2507,J2510,J2513,J2515,J2540,J2543,J2545,J2550,J2560,J2562,J2590,J2597,J2650,J2670,J2675,J2680,J2690,J2700,J2704,J2710,J2720,J2724,J2725,J2730,J2760,J2765,J2770,J2778,J2780,J2783,J2785,J2788,J2790,J2791,J2792,J2793,J2794,J2795,J2796,J2800,J2805,J2810,J2820,J2850,J2910,J2916,J2920,J2930,J2940,J2941,J2950,J2993,J2995,J2997,J3000,J3010,J3030,J3060,J3070,J3095,J3101,J3105,J3110,J3120,J3121,J3130,J3140,J3145,J3150,J3230,J3240,J3243,J3246,J3250,J3260,J3262,J3265,J3280,J3285,J3300,J3301,J3302,J3303,J3305,J3310,J3315,J3320,J3350,J3355,J3357,J3360,J3364,J3365,J3370,J3385,J3396,J3400,J3410,J3411,J3415,J3420,J3430,J3465,J3470,J3471,J3472,J3473,J3475,J3480,J3485,J3486,J3487,J3488,J3489,J3490,J3520,J3530,J3535,J3570,J3590,J7030,J7040,J7042,J7050,J7060,J7070,J7100,J7110,J7120,J7130,J7131,J7178,J7180,J7181,J7182,J7183,J7184,J7185,J7186,J7187,J7189,J7190,J7191,J7192,J7193,J7194,J7195,J7196,J7197,J7198,J7199,J7200,J7201,J7300,J7301,J7302,J7303,J7304,J7306,J7307,J7308,J7309,J7310,J7311,J7312,J7315,J7316,J7321,J7323,J7324,J7325,J7326,J7327,J7330,J7335,J7336,J7500,J7501,J7502,J7504,J7505,J7506,J7507,J7508,J7509,J7510,J7511,J7513,J7515,J7516,J7517,J7518,J7520,J7525,J7527,J7599,J7604,J7605,J7606,J7607,J7608,J7609,J7610,J7611,J7612,J7613,J7614,J7615,J7620,J7622,J7624,J7626,J7627,J7628,J7629,J7631,J7632,J7633,J7634,J7635,J7636,J7637,J7638,J7639,J7640,J7641,J7642,J7643,J7644,J7645," +
",J7647,J7648,J7649,J7650,J7657,J7658,J7659,J7660,J7665,J7667,J7668,J7669,J7670,J7674,J7676,J7680,J7681,J7682,J7683,J7684,J7685,J7686,J7699,J7799,J8498,J8499,J8501,J8510,J8515,J8520,J8521,J8530,J8540,J8560,J8561,J8562,J8565,J8597,J8600,J8610,J8650,J8700,J8705,J8999,J9000,J9001,J9002,J9010,J9015,J9017,J9019,J9020,J9025,J9027,J9031,J9033,J9035,J9040,J9041,J9042,J9043,J9045,J9047,J9050,J9055,J9060,J9062,J9065,J9070,J9080,J9090,J9091,J9092,J9093,J9094,J9095,J9096,J9097,J9098,J9100,J9110,J9120,J9130,J9140,J9150,J9151,J9155,J9160,J9165,J9171,J9175,J9178,J9179,J9181,J9185,J9190,J9200,J9201,J9202,J9206,J9207,J9208,J9209,J9211,J9212,J9213,J9214,J9215,J9216,J9217,J9218,J9219,J9225,J9226,J9228,J9230,J9245,J9250,J9260,J9261,J9262,J9263,J9264,J9265,J9266,J9267,J9268,J9270,J9280,J9290,J9291,J9293,J9300,J9301,J9302,J9303,J9305,J9306,J9307,J9310,J9315,J9320,J9328,J9330,J9340,J9350,J9351,J9354,J9355,J9357,J9360,J9370,J9371,J9375,J9380,J9390,J9395,J9400,J9600,J9999,K0001,K0002,K0003,K0004,K0005,K0006,K0007,K0008,K0009,K0010,K0011,K0012,K0013,K0014,K0015,K0017,K0018,K0019,K0020,K0037,K0038,K0039,K0040,K0041,K0042,K0043,K0044,K0045,K0046,K0047,K0050,K0051,K0052,K0053,K0056,K0065,K0069,K0070,K0071,K0072,K0073,K0077,K0098,K0105,K0108,K0195,K0455,K0462,K0552,K0601,K0602,K0603,K0604,K0605,K0606,K0607,K0608,K0609,K0669,K0672,K0730,K0733,K0734,K0735,K0736,K0737,K0738,K0739,K0740,K0741,K0742,K0743,K0744,K0745,K0746,K0800,K0801,K0802,K0806,K0807,K0808,K0812,K0813,K0814,K0815,K0816,K0820,K0821,K0822,K0823,K0824,K0825,K0826,K0827,K0828,K0829," +
",K0830,K0831,K0835,K0836,K0837,K0838,K0839,K0840,K0841,K0842,K0843,K0848,K0849,K0850,K0851,K0852,K0853,K0854,K0855,K0856,K0857,K0858,K0859,K0860,K0861,K0862,K0863,K0864,K0868,K0869,K0870,K0871,K0877,K0878,K0879,K0880,K0884,K0885,K0886,K0890,K0891,K0898,K0899,K0900,K0901,K0902,L0112,L0113,L0120,L0130,L0140,L0150,L0160,L0170,L0172,L0174,L0180,L0190,L0200,L0220,L0430,L0450,L0452,L0454,L0455,L0456,L0457,L0458,L0460,L0462,L0464,L0466,L0467,L0468,L0469,L0470,L0472,L0480,L0482,L0484,L0486,L0488,L0490,L0491,L0492,L0621,L0622,L0623,L0624,L0625,L0626,L0627,L0628,L0629,L0630,L0631,L0632,L0633,L0634,L0635,L0636,L0637,L0638,L0639,L0640,L0641,L0642,L0643,L0648,L0649,L0650,L0651,L0700,L0710,L0810,L0820,L0830,L0859,L0861,L0970,L0972,L0974,L0976,L0978,L0980,L0982,L0984,L0999,L1000,L1001,L1005,L1010,L1020,L1025,L1030,L1040,L1050,L1060,L1070,L1080,L1085,L1090,L1100,L1110,L1120,L1200,L1210,L1220,L1230,L1240,L1250,L1260,L1270,L1280,L1290,L1300,L1310,L1499,L1500,L1510,L1520,L1600,L1610,L1620,L1630,L1640,L1650,L1652,L1660,L1680,L1685,L1686,L1690,L1700,L1710,L1720,L1730,L1755,L1810,L1812,L1820,L1830,L1831,L1832,L1833,L1834,L1836,L1840,L1843,L1844,L1845,L1846,L1847,L1848,L1850,L1860,L1900,L1902,L1904,L1906,L1907,L1910,L1920,L1930,L1932,L1940,L1945,L1950,L1951,L1960,L1970,L1971,L1980,L1990,L2000,L2005,L2010,L2020,L2030,L2034,L2035,L2036,L2037,L2038,L2040,L2050,L2060,L2070,L2080,L2090,L2106,L2108,L2112,L2114,L2116,L2126,L2128,L2132,L2134,L2136,L2180,L2182,L2184,L2186,L2188,L2190,L2192,L2200,L2210,L2220,L2230,L2232,L2240,L2250,L2260,L2265," +
",L2270,L2275,L2280,L2300,L2310,L2320,L2330,L2335,L2340,L2350,L2360,L2370,L2375,L2380,L2385,L2387,L2390,L2395,L2397,L2405,L2415,L2425,L2430,L2492,L2500,L2510,L2520,L2525,L2526,L2530,L2540,L2550,L2570,L2580,L2600,L2610,L2620,L2622,L2624,L2627,L2628,L2630,L2640,L2650,L2660,L2670,L2680,L2750,L2755,L2760,L2768,L2780,L2785,L2795,L2800,L2810,L2820,L2830,L2840,L2850,L2861,L2999,L3000,L3001,L3002,L3003,L3010,L3020,L3030,L3031,L3040,L3050,L3060,L3070,L3080,L3090,L3100,L3140,L3150,L3160,L3170,L3201,L3202,L3203,L3204,L3206,L3207,L3208,L3209,L3211,L3212,L3213,L3214,L3215,L3216,L3217,L3219,L3221,L3222,L3224,L3225,L3230,L3250,L3251,L3252,L3253,L3254,L3255,L3257,L3260,L3265,L3300,L3310,L3320,L3330,L3332,L3334,L3340,L3350,L3360,L3370,L3380,L3390,L3400,L3410,L3420,L3430,L3440,L3450,L3455,L3460,L3465,L3470,L3480,L3485,L3500,L3510,L3520,L3530,L3540,L3550,L3560,L3570,L3580,L3590,L3595,L3600,L3610,L3620,L3630,L3640,L3649,L3650,L3660,L3670,L3671,L3672,L3673,L3674,L3675,L3677,L3678,L3702,L3710,L3720,L3730,L3740,L3760,L3762,L3763,L3764,L3765,L3766,L3806,L3807,L3808,L3809,L3891,L3900,L3901,L3904,L3905,L3906,L3908,L3912,L3913,L3915,L3916,L3917,L3918,L3919,L3921,L3923,L3924,L3925,L3927,L3929,L3930,L3931,L3933,L3935,L3956,L3960,L3961,L3962,L3964,L3965,L3966,L3967,L3968,L3969,L3970,L3971,L3972,L3973,L3974,L3975,L3976,L3977,L3978,L3980,L3981,L3982,L3984,L3995,L3999,L4000,L4002,L4010,L4020,L4030,L4040,L4045,L4050,L4055,L4060,L4070,L4080,L4090,L4100,L4110,L4130,L4205,L4210,L4350,L4360,L4361,L4370,L4380,L4386,L4387,L4392,L4394,L4396,L4397,L4398," +
",L4631,L5000,L5010,L5020,L5050,L5060,L5100,L5105,L5150,L5160,L5200,L5210,L5220,L5230,L5250,L5270,L5280,L5301,L5311,L5312,L5321,L5331,L5341,L5400,L5410,L5420,L5430,L5450,L5460,L5500,L5505,L5510,L5520,L5530,L5535,L5540,L5560,L5570,L5580,L5585,L5590,L5595,L5600,L5610,L5611,L5613,L5614,L5616,L5617,L5618,L5620,L5622,L5624,L5626,L5628,L5629,L5630,L5631,L5632,L5634,L5636,L5637,L5638,L5639,L5640,L5642,L5643,L5644,L5645,L5646,L5647,L5648,L5649,L5650,L5651,L5652,L5653,L5654,L5655,L5656,L5658,L5661,L5665,L5666,L5668,L5670,L5671,L5672,L5673,L5676,L5677,L5678,L5679,L5680,L5681,L5682,L5683,L5684,L5685,L5686,L5688,L5690,L5692,L5694,L5695,L5696,L5697,L5698,L5699,L5700,L5701,L5702,L5703,L5704,L5705,L5706,L5707,L5710,L5711,L5712,L5714,L5716,L5718,L5722,L5724,L5726,L5728,L5780,L5781,L5782,L5785,L5790,L5795,L5810,L5811,L5812,L5814,L5816,L5818,L5822,L5824,L5826,L5828,L5830,L5840,L5845,L5848,L5850,L5855,L5856,L5857,L5858,L5859,L5910,L5920,L5925,L5930,L5940,L5950,L5960,L5961,L5962,L5964,L5966,L5968,L5969,L5970,L5971,L5972,L5973,L5974,L5975,L5976,L5978,L5979,L5980,L5981,L5982,L5984,L5985,L5986,L5987,L5988,L5990,L5999,L6000,L6010,L6020,L6025,L6026,L6050,L6055,L6100,L6110,L6120,L6130,L6200,L6205,L6250,L6300,L6310,L6320,L6350,L6360,L6370,L6380,L6382,L6384,L6386,L6388,L6400,L6450,L6500,L6550,L6570,L6580,L6582,L6584,L6586,L6588,L6590,L6600,L6605,L6610,L6611,L6615,L6616,L6620,L6621,L6623,L6624,L6625,L6628,L6629,L6630,L6632,L6635,L6637,L6638,L6640,L6641,L6642,L6645,L6646,L6647,L6648,L6650,L6655,L6660,L6665,L6670,L6672,L6675,L6676,L6677,L6680," +
",L6682,L6684,L6686,L6687,L6688,L6689,L6690,L6691,L6692,L6693,L6694,L6695,L6696,L6697,L6698,L6703,L6704,L6706,L6707,L6708,L6709,L6711,L6712,L6713,L6714,L6715,L6721,L6722,L6805,L6810,L6880,L6881,L6882,L6883,L6884,L6885,L6890,L6895,L6900,L6905,L6910,L6915,L6920,L6925,L6930,L6935,L6940,L6945,L6950,L6955,L6960,L6965,L6970,L6975,L7007,L7008,L7009,L7040,L7045,L7170,L7180,L7181,L7185,L7186,L7190,L7191,L7259,L7260,L7261,L7266,L7272,L7274,L7360,L7362,L7364,L7366,L7367,L7368,L7400,L7401,L7402,L7403,L7404,L7405,L7499,L7500,L7510,L7520,L7600,L7900,L7902,L8000,L8001,L8002,L8010,L8015,L8020,L8030,L8031,L8032,L8035,L8039,L8040,L8041,L8042,L8043,L8044,L8045,L8046,L8047,L8048,L8049,L8300,L8310,L8320,L8330,L8400,L8410,L8415,L8417,L8420,L8430,L8435,L8440,L8460,L8465,L8470,L8480,L8485,L8499,L8500,L8501,L8505,L8507,L8509,L8510,L8511,L8512,L8513,L8514,L8515,L8600,L8603,L8604,L8605,L8606,L8609,L8610,L8612,L8613,L8614,L8615,L8616,L8617,L8618,L8619,L8621,L8622,L8623,L8624,L8627,L8628,L8629,L8630,L8631,L8641,L8642,L8658,L8659,L8670,L8679,L8680,L8681,L8682,L8683,L8684,L8685,L8686,L8687,L8688,L8689,L8690,L8691,L8692,L8693,L8695,L8696,L8699,L9900,P2028,P2029,P2031,P2033,P2038,P3000,P3001,P7001,P9010,P9011,P9012,P9016,P9017,P9019,P9020,P9021,P9022,P9023,P9031,P9032,P9033,P9034,P9035,P9036,P9037,P9038,P9039,P9040,P9041,P9043,P9044,P9045,P9046,P9047,P9048,P9050,P9051,P9052,P9053,P9054,P9055,P9056,P9057,P9058,P9059,P9060,P9603,P9604,P9612,P9615,Q0035,Q0081,Q0083,Q0084,Q0085,Q0090,Q0091,Q0092,Q0111,Q0112,Q0113,Q0114,Q0115,Q0138,Q0139,Q0144,Q0161," +
",Q0162,Q0163,Q0164,Q0165,Q0166,Q0167,Q0168,Q0169,Q0170,Q0171,Q0172,Q0173,Q0174,Q0175,Q0176,Q0177,Q0178,Q0179,Q0180,Q0181,Q0478,Q0479,Q0480,Q0481,Q0482,Q0483,Q0484,Q0485,Q0486,Q0487,Q0488,Q0489,Q0490,Q0491,Q0492,Q0493,Q0494,Q0495,Q0496,Q0497,Q0498,Q0499,Q0500,Q0501,Q0502,Q0503,Q0504,Q0505,Q0506,Q0507,Q0508,Q0509,Q0510,Q0511,Q0512,Q0513,Q0514,Q0515,Q1003,Q1004,Q1005,Q2004,Q2009,Q2017,Q2025,Q2026,Q2027,Q2028,Q2033,Q2034,Q2035,Q2036,Q2037,Q2038,Q2039,Q2040,Q2041,Q2042,Q2043,Q2044,Q2045,Q2046,Q2047,Q2048,Q2049,Q2050,Q2051,Q2052,Q3001,Q3014,Q3025,Q3026,Q3027,Q3028,Q3031,Q4001,Q4002,Q4003,Q4004,Q4005,Q4006,Q4007,Q4008,Q4009,Q4010,Q4011,Q4012,Q4013,Q4014,Q4015,Q4016,Q4017,Q4018,Q4019,Q4020,Q4021,Q4022,Q4023,Q4024,Q4025,Q4026,Q4027,Q4028,Q4029,Q4030,Q4031,Q4032,Q4033,Q4034,Q4035,Q4036,Q4037,Q4038,Q4039,Q4040,Q4041,Q4042,Q4043,Q4044,Q4045,Q4046,Q4047,Q4048,Q4049,Q4050,Q4051,Q4074,Q4081,Q4082,Q4100,Q4101,Q4102,Q4103,Q4104,Q4105,Q4106,Q4107,Q4108,Q4109,Q4110,Q4111,Q4112,Q4113,Q4114,Q4115,Q4116,Q4117,Q4118,Q4119,Q4120,Q4121,Q4122,Q4123,Q4124,Q4125,Q4126,Q4127,Q4128,Q4129,Q4130,Q4131,Q4132,Q4133,Q4134,Q4135,Q4136,Q4137,Q4138,Q4139,Q4140,Q4141,Q4142,Q4143,Q4145,Q4146,Q4147,Q4148,Q4149,Q4150,Q4151,Q4152,Q4153,Q4154,Q4155,Q4156,Q4157,Q4158,Q4159,Q4160,Q5001,Q5002,Q5003,Q5004,Q5005,Q5006,Q5007,Q5008,Q5009,Q5010,Q9951,Q9953,Q9954,Q9955,Q9956,Q9957,Q9958,Q9959,Q9960,Q9961,Q9962,Q9963,Q9964,Q9965,Q9966,Q9967,Q9968,Q9969,Q9970,Q9972,Q9973,Q9974,Q9975,R0070,R0075,R0076,S0012,S0014,S0017,S0020,S0021,S0023,S0028,S0030,S0032,S0034,S0039," +
",S0040,S0073,S0074,S0077,S0078,S0080,S0081,S0088,S0090,S0091,S0092,S0093,S0104,S0106,S0108,S0109,S0117,S0119,S0122,S0126,S0128,S0132,S0136,S0137,S0138,S0139,S0140,S0142,S0144,S0145,S0146,S0148,S0155,S0156,S0157,S0160,S0161,S0164,S0166,S0169,S0170,S0171,S0172,S0174,S0175,S0176,S0177,S0178,S0179,S0181,S0182,S0183,S0187,S0189,S0190,S0191,S0194,S0195,S0196,S0197,S0199,S0207,S0208,S0209,S0215,S0250,S0255,S0257,S0270,S0271,S0272,S0280,S0281,S0302,S0315,S0316,S0317,S0320,S0340,S0341,S0342,S0395,S0500,S0504,S0506,S0508,S0510,S0512,S0514,S0515,S0516,S0518,S0580,S0581,S0590,S0592,S0595,S0596,S0601,S0618,S0622,S0625,S1001,S1002,S1015,S1016,S1030,S1031,S1034,S1035,S1036,S1037,S1040,S1090,S2055,S2061,S2140,S2142,S2260,S2265,S2266,S2267,S2270,S3005,S3600,S3601,S3620,S3625,S3626,S3628,S3630,S3645,S3650,S3652,S3655,S3708,S3711,S3713,S3721,S3722,S3800,S3818,S3819,S3820,S3822,S3823,S3828,S3829,S3830,S3831,S3833,S3834,S3835,S3837,S3840,S3841,S3842,S3843,S3844,S3845,S3846,S3847,S3848,S3849,S3850,S3851,S3852,S3853,S3854,S3855,S3860,S3861,S3862,S3865,S3866,S3870,S3890,S3900,S3902,S3904,S3905,S4005,S4011,S4015,S4016,S4017,S4018,S4020,S4021,S4022,S4023,S4025,S4026,S4027,S4030,S4031,S4035,S4037,S4040,S4042,S4981,S4990,S4991,S4995,S5000,S5001,S5010,S5011,S5012,S5013,S5014,S5035,S5036,S5100,S5101,S5102,S5105,S5108,S5109,S5110,S5111,S5115,S5116,S5120,S5121,S5125,S5126,S5130,S5131,S5135,S5136,S5140,S5141,S5145,S5146,S5150,S5151,S5160,S5161,S5162,S5165,S5170,S5175,S5180,S5181,S5185,S5190,S5199,S5497,S5498,S5501,S5502,S5517,S5518,S5520,S5521," +
",S5550,S5551,S5552,S5553,S5560,S5561,S5565,S5566,S5570,S5571,S8030,S8032,S8035,S8037,S8040,S8042,S8049,S8055,S8080,S8085,S8092,S8096,S8097,S8100,S8101,S8110,S8120,S8121,S8130,S8131,S8185,S8186,S8189,S8210,S8262,S8265,S8270,S8301,S8415,S8420,S8421,S8422,S8423,S8424,S8425,S8426,S8427,S8428,S8429,S8430,S8431,S8450,S8451,S8452,S8460,S8490,S8930,S8940,S8948,S8950,S8990,S8999,S9001,S9007,S9015,S9024,S9025,S9055,S9056,S9061,S9075,S9083,S9088,S9090,S9097,S9098,S9109,S9110,S9117,S9122,S9123,S9124,S9125,S9126,S9127,S9140,S9141,S9150,S9208,S9209,S9211,S9212,S9213,S9214,S9325,S9326,S9327,S9328,S9329,S9330,S9331,S9335,S9336,S9338,S9339,S9340,S9341,S9342,S9343,S9345,S9346,S9347,S9348,S9349,S9351,S9353,S9355,S9357,S9359,S9361,S9363,S9364,S9365,S9366,S9367,S9368,S9370,S9372,S9373,S9374,S9375,S9376,S9377,S9379,S9381,S9401,S9430,S9433,S9434,S9435,S9436,S9437,S9438,S9439,S9441,S9442,S9443,S9444,S9445,S9446,S9447,S9449,S9451,S9452,S9453,S9454,S9455,S9460,S9465,S9470,S9472,S9473,S9474,S9475,S9476,S9480,S9482,S9484,S9485,S9490,S9494,S9497,S9500,S9501,S9502,S9503,S9504,S9529,S9537,S9538,S9542,S9558,S9559,S9560,S9562,S9590,S9810,S9900,S9901,S9960,S9961,S9970,S9975,S9976,S9977,S9981,S9982,S9986,S9988,S9989,S9990,S9991,S9992,S9994,S9996,S9999,T1000,T1001,T1002,T1003,T1004,T1005,T1006,T1007,T1009,T1010,T1012,T1013,T1014,T1015,T1016,T1017,T1018,T1019,T1020,T1021,T1022,T1023,T1027,T1028,T1029,T1030,T1031,T1502,T1503,T1505,T1999,T2001,T2002,T2003,T2004,T2005,T2007,T2010,T2011,T2012,T2013,T2014,T2015,T2016,T2017,T2018,T2019,T2020,T2021,T2022," +
",T2023,T2024,T2025,T2026,T2027,T2028,T2029,T2030,T2031,T2032,T2033,T2034,T2035,T2036,T2037,T2038,T2039,T2040,T2041,T2042,T2043,T2044,T2045,T2046,T2048,T2049,T2101,T4521,T4522,T4523,T4524,T4525,T4526,T4527,T4528,T4529,T4530,T4531,T4532,T4533,T4534,T4535,T4536,T4537,T4538,T4539,T4540,T4541,T4542,T4543,T4544,T5001,T5999,V2020,V2025,V2100,V2101,V2102,V2103,V2104,V2105,V2106,V2107,V2108,V2109,V2110,V2111,V2112,V2113,V2114,V2115,V2118,V2121,V2199,V2200,V2201,V2202,V2203,V2204,V2205,V2206,V2207,V2208,V2209,V2210,V2211,V2212,V2213,V2214,V2215,V2218,V2219,V2220,V2221,V2299,V2300,V2301,V2302,V2303,V2304,V2305,V2306,V2307,V2308,V2309,V2310,V2311,V2312,V2313,V2314,V2315,V2318,V2319,V2320,V2321,V2399,V2410,V2430,V2499,V2500,V2501,V2502,V2503,V2510,V2511,V2512,V2513,V2520,V2521,V2522,V2523,V2530,V2531,V2599,V2600,V2610,V2615,V2623,V2624,V2625,V2626,V2627,V2628,V2629,V2630,V2631,V2632,V2700,V2702,V2710,V2715,V2718,V2730,V2744,V2745,V2750,V2755,V2756,V2760,V2761,V2762,V2770,V2780,V2781,V2782,V2783,V2784,V2785,V2786,V2787,V2788,V2790,V2797,V2799,V5008,V5010,V5011,V5014,V5020,V5030,V5040,V5050,V5060,V5070,V5080,V5090,V5095,V5100,V5110,V5120,V5130,V5140,V5150,V5160,V5170,V5180,V5190,V5200,V5210,V5220,V5230,V5240,V5241,V5242,V5243,V5244,V5245,V5246,V5247,V5248,V5249,V5250,V5251,V5252,V5253,V5254,V5255,V5256,V5257,V5258,V5259,V5260,V5261,V5262,V5263,V5264,V5265,V5266,V5267,V5268,V5269,V5270,V5271,V5272,V5273,V5274,V5275,V5281,V5282,V5283,V5284,V5285,V5286,V5287,V5288,V5289,V5290,V5298,V5299,V5336,V5362,V5363,V5364";

            string allVALIDscs = "00100,00102,00103,00104,00120,00124,00126,00140,00142,00144,00145,00147,00148,00160,00162,00164,0016T,00170,00172,00174,00176,0017T,00190,00192,0019T,00210,00211,00212,00214,00215,00216,00218,00220,00222,00300,00320,00322,00326,00350,00352,00400,00402,00404,00406,00410,00450,00452,00454,00470,00472,00474,00500,0051T,00520,00522,00524,00528,00529,0052T,00530,00532,00534,00537,00539,0053T,00540,00541,00542,00546,00548,0054T,00550,0055T,00560,00561,00562,00563,00566,00567,00580,00600,00604,00620,00622,00625,00626,00630,00632,00634,00635,00640,00670,00700,00702,0071T,0072T,00730,0073T,00740,00750,00752,00754,00756,0075T,0076T,00770,00790,00792,00794,00796,00797,00800,00802,00810,00820,00830,00832,00834,00836,00840,00842,00844,00846,00848,00851,00860,00862,00864,00865,00866,00868,00870,00872,00873,00880,00882,00902,00904,00906,00908,00910,00912,00914,00916,00918,00920,00921,00922,00924,00926,00928,0092T,00930,00932,00934,00936,00938,00940,00942,00944,00948,00950,00952,0095T,0098T,0099T,0100T,0101T,0102T,01112,01120,01130,01140,01150,01160,01170,01173,01180,01190,01200,01202,01210,01212,01214,01215,01220,01230,01232,01234,0123T,01250,01260,01270,01272,01274,01320,01340,01360,01380,01382,01390,01392,01400,01402,01404,0141T,01420,0142T,01430,01432,0143T,01440,01442,01444,01462,01464,01470,01472,01474,01480,01482,01484,01486,01490,01500,01502,01520,01522,0155T,0156T,0157T,0158T,0160T,01610,0161T,01620,01622,01630,01634,01636,01638,0163T,0164T,01650,01652,01654,01656,0165T,0166T,01670,0167T,01680,01682,01710,01712,01714,01716,0171T,0172T,01730,01732,01740,01742,01744,01756,01758,01760,0176T,01770,01772,0177T,01780,01782,01810,01820,01829,0182T,01830,01832,01840,01842,01844,0184T,01850,01852,01860,0188T,0189T,01916,0191T,01920,01922,01924,01925,01926,01930,01931,01932,01933,01935,01936,0193T,01951,01952,01953,01958,0195T,01960,01961,01962,01963,01965,01967,01968,01969,0196T,01990,01991,01992,01996,01999,0200T,0201T,0202T,0207T,0208T,0209T,0210T,0211T,0212T,0213T,0214T,0215T,0216T,0217T,0218T,0219T,0220T,0221T,0222T,0228T,0229T,0230T,0231T,0234T,0235T,0236T,0237T,0238T,0245T,0246T,0247T,0248T,0249T,0253T,0254T,0262T,0263T,0264T,0265T,0266T,0267T,0268T,0269T,0270T,0271T,0274T,0275T,0278T,0281T,0282T,0283T,0284T,0285T,0288T,0290T,0293T,0294T,0299T,0300T,0301T,0302T,0303T,0304T,0307T,0308T,0309T,0310T,0312T,0313T,0314T,0315T,0316T,0317T,0319T,0320T,0321T,0322T,0323T,0324T,0325T,0334T,0335T,0336T,0338T,0339T,0340T,0342T,0343T,0344T,0345T,10021,10022,10030,10040,10060,10061,10080,10081,10120,10121,10140,10160,10180,11000,11001,11004,11005,11006,11008,11010,11011,11012,11040,11041,11042,11043,11044,11045,11046,11047,11055,11056,11057,11100,11101,11200,11201,11300,11301,11302,11303,11305,11306,11307,11308,11310,11311,11312,11313,11400,11401,11402,11403,11404,11406,11420,11421,11422,11423,11424,11426,11440,11441,11442,11443,11444,11446,11450,11451,11462,11463,11470,11471,11600,11601,11602,11603,11604,11606,11620,11621,11622,11623,11624,11626,11640,11641,11642,11643,11644,11646,11719,11720,11721,11730,11732,11740,11750,11752,11755,11760,11762,11765,11770,11771,11772,11900,11901,11920,11921,11922,11950,11951,11952,11954,11960,11970,11971,11975,11976,11977,11980,11981,11982,11983,12001,12002,12004,12005,12006,12007,12011,12013,12014,12015,12016,12017,12018,12020,12021,12031,12032,12034,12035,12036,12037,12041,12042,12044,12045,12046,12047,12051,12052,12053,12054,12055,12056,12057,13100,13101,13102,13120,13121,13122,13131,13132,13133,13151,13152,13153,13160,14000,14001,14020,14021,14040,14041,14060,14061,14301,14302,14350,15002,15003,15004,15005,15040,15050,15100,15101,15110,15111,15115,15116,15120,15121,15130,15131,15135,15136,15150,15151,15152,15155,15156,15157,15170,15171,15175,15176,15200,15201,15220,15221,15240,15241,15260,15261,15271,15272,15273,15274,15275,15276,15277,15278,15300,15301,15320,15321,15330,15331,15335,15336,15340,15341,15360,15361,15365,15366,15400,15401,15420,15421,15430,15431,15570,15572,15574,15576,15600,15610,15620,15630,15650,15731,15732,15734,15736,15738,15740,15750,15756,15757,15758,15760,15770,15775,15776,15777,15780,15781,15782,15783,15786,15787,15788,15789,15792,15793,15819,15820,15821,15822,15823,15824,15825,15826,15828,15829,15830,15832,15833,15834,15835,15836,15837,15838,15839,15840,15841,15842,15845,15847,15850,15851,15852,15860,15876,15877,15878,15879,15920,15922,15931,15933,15934,15935,15936,15937,15940,15941,15944,15945,15946,15950,15951,15952,15953,15956,15958,15999,16000,16020,16025,16030,16035,16036,17000,17003,17004,17106,17107,17108,17110,17111,17250,17260,17261,17262,17263,17264,17266,17270,17271,17272,17273,17274,17276,17280,17281,17282,17283,17284,17286,17311,17312,17313,17314,17315,17340,17360,17380,17999,19000,19001,19020,19030,19081,19082,19083,19084,19085,19086,19100,19101,19105,19110,19112,19120,19125,19126,19260,19271,19272,19281,19282,19283,19284,19285,19286,19287,19288,19296,19297,19298,19300,19301,19302,19303,19304,19305,19306,19307,19316,19318,19324,19325,19328,19330,19340,19342,19350,19355,19357,19361,19364,19366,19367,19368,19369,19370,19371,19380,19396,19499,20000,20005,20100,20101,20102,20103,20150,20200,20205,20206,20220,20225,20240,20245,20250,20251,20500,20501,20520,20525,20526,20527,20550,20551,20552,20553,20555,20600,20605,20610,20612,20615,20650,20660,20661,20662,20663,20664,20665,20670,20680,20690,20692,20693,20694,20696,20697,20802,20805,20808,20816,20822,20824,20827,20838,20900,20902,20910,20912,20920,20922,20924,20926,20930,20931,20936,20937,20938,20950,20955,20956,20957,20962,20969,20970,20972,20973,20974,20975,20979,20982,20985,20999,21010,21011,21012,21013,21014,21015,21016,21025,21026,21029,21030,21031,21032,21034,21040,21044,21045,21046,21047,21048,21049,21050,21060,21070,21073,21076,21077,21079,21080,21081,21082,21083,21084,21085,21086,21087,21088,21089,21100,21110,21116,21120,21121,21122,21123,21125,21127,21137,21138,21139,21141,21142,21143,21145,21146,21147,21150,21151,21154,21155,21159,21160,21172,21175,21179,21180,21181,21182,21183,21184,21188,21193,21194,21195,21196,21198,21199,21206,21208,21209,21210,21215,21230,21235,21240,21242,21243,21244,21245,21246,21247,21248,21249,21255,21256,21260,21261,21263,21267,21268,21270,21275,21280,21282,21295,21296,21299,21310,21315,21320,21325,21330,21335,21336,21337,21338,21339,21340,21343,21344,21345,21346,21347,21348,21355,21356,21360,21365,21366,21385,21386,21387,21390,21395,21400,21401,21406,21407,21408,21421,21422,21423,21431,21432,21433,21435,21436,21440,21445,21450,21451,21452,21453,21454,21461,21462,21465,21470,21480,21485,21490,21495,21497,21499,21501,21502,21510,21550,21552,21554,21555,21556,21557,21558,21600,21610,21615,21616,21620,21627,21630,21632,21685,21700,21705,21720,21725,21740,21742,21743,21750,21800,21805,21810,21820,21825,21899,21920,21925,21930,21931,21932,21933,21935,21936,22010,22015,22100,22101,22102,22103,22110,22112,22114,22116,22206,22207,22208,22210,22212,22214,22216,22220,22222,22224,22226,22305,22310,22315,22318,22319,22325,22326,22327,22328,22505,22520,22521,22522,22523,22524,22525,22526,22527,22532,22533,22534,22548,22551,22552,22554,22556,22558,22585,22586,22590,22595,22600,22610,22612,22614,22630,22632,22633,22634,22800,22802,22804,22808,22810,22812,22818,22819,22830,22840,22841,22842,22843,22844,22845,22846,22847,22848,22849,22850,22851,22852,22855,22856,22857,22861,22862,22864,22865,22899,22900,22901,22902,22903,22904,22905,22999,23000,23020,23030,23031,23035,23040,23044,23065,23066,23071,23073,23075,23076,23077,23078,23100,23101,23105,23106,23107,23120,23125,23130,23140,23145,23146,23150,23155,23156,23170,23172,23174,23180,23182,23184,23190,23195,23200,23210,23220,23330,23333,23334,23335,23350,23395,23397,23400,23405,23406,23410,23412,23415,23420,23430,23440,23450,23455,23460,23462,23465,23466,23470,23472,23473,23474,23480,23485,23490,23491,23500,23505,23515,23520,23525,23530,23532,23540,23545,23550,23552,23570,23575,23585,23600,23605,23615,23616,23620,23625,23630,23650,23655,23660,23665,23670,23675,23680,23700,23800,23802,23900,23920,23921,23929,23930,23931,23935,24000,24006,24065,24066,24071,24073,24075,24076,24077,24079,24100,24101,24102,24105,24110,24115,24116,24120,24125,24126,24130,24134,24136,24138,24140,24145,24147,24149,24150,24152,24155,24160,24164,24200,24201,24220,24300,24301,24305,24310,24320,24330,24331,24332,24340,24341,24342,24343,24344,24345,24346,24357,24358,24359,24360,24361,24362,24363,24365,24366,24370,24371,24400,24410,24420,24430,24435,24470,24495,24498,24500,24505,24515,24516,24530,24535,24538,24545,24546,24560,24565,24566,24575,24576,24577,24579,24582,24586,24587,24600,24605,24615,24620,24635,24640,24650,24655,24665,24666,24670,24675,24685,24800,24802,24900,24920,24925,24930,24931,24935,24940,24999,25000,25001,25020,25023,25024,25025,25028,25031,25035,25040,25065,25066,25071,25073,25075,25076,25077,25078,25085,25100,25101,25105,25107,25109,25110,25111,25112,25115,25116,25118,25119,25120,25125,25126,25130,25135,25136,25145,25150,25151,25170,25210,25215,25230,25240,25246,25248,25250,25251,25259,25260,25263,25265,25270,25272,25274,25275,25280,25290,25295,25300,25301,25310,25312,25315,25316,25320,25332,25335,25337,25350,25355,25360,25365,25370,25375,25390,25391,25392,25393,25394,25400,25405,25415,25420,25425,25426,25430,25431,25440,25441,25442,25443,25444,25445,25446,25447,25449,25450,25455,25490,25491,25492,25500,25505,25515,25520,25525,25526,25530,25535,25545,25560,25565,25574,25575,25600,25605,25606,25607,25608,25609,25622,25624,25628,25630,25635,25645,25650,25651,25652,25660,25670,25671,25675,25676,25680,25685,25690,25695,25800,25805,25810,25820,25825,25830,25900,25905,25907,25909,25915,25920,25922,25924,25927,25929,25931,25999,26010,26011,26020,26025,26030,26034,26035,26037,26040,26045,26055,26060,26070,26075,26080,26100,26105,26110,26111,26113,26115,26116,26117,26118,26121,26123,26125,26130,26135,26140,26145,26160,26170,26180,26185,26200,26205,26210,26215,26230,26235,26236,26250,26260,26262,26320,26340,26341,26350,26352,26356,26357,26358,26370,26372,26373,26390,26392,26410,26412,26415,26416,26418,26420,26426,26428,26432,26433,26434,26437,26440,26442,26445,26449,26450,26455,26460,26471,26474,26476,26477,26478,26479,26480,26483,26485,26489,26490,26492,26494,26496,26497,26498,26499,26500,26502,26508,26510,26516,26517,26518,26520,26525,26530,26531,26535,26536,26540,26541,26542,26545,26546,26548,26550,26551,26553,26554,26555,26556,26560,26561,26562,26565,26567,26568,26580,26587,26590,26591,26593,26596,26600,26605,26607,26608,26615,26641,26645,26650,26665,26670,26675,26676,26685,26686,26700,26705,26706,26715,26720,26725,26727,26735,26740,26742,26746,26750,26755,26756,26765,26770,26775,26776,26785,26820,26841,26842,26843,26844,26850,26852,26860,26861,26862,26863,26910,26951,26952,26989,26990,26991,26992,27000,27001,27003,27005,27006,27025,27027,27030,27033,27035,27036,27040,27041,27043,27045,27047,27048,27049,27050,27052,27054,27057,27059,27060,27062,27065,27066,27067,27070,27071,27075,27076,27077,27078,27080,27086,27087,27090,27091,27093,27095,27096,27097,27098,27100,27105,27110,27111,27120,27122,27125,27130,27132,27134,27137,27138,27140,27146,27147,27151,27156,27158,27161,27165,27170,27175,27176,27177,27178,27179,27181,27185,27187,27193,27194,27200,27202,27215,27216,27217,27218,27220,27222,27226,27227,27228,27230,27232,27235,27236,27238,27240,27244,27245,27246,27248,27250,27252,27253,27254,27256,27257,27258,27259,27265,27266,27267,27268,27269,27275,27280,27282,27284,27286,27290,27295,27299,27301,27303,27305,27306,27307,27310,27323,27324,27325,27326,27327,27328,27329,27330,27331,27332,27333,27334,27335,27337,27339,27340,27345,27347,27350,27355,27356,27357,27358,27360,27364,27365,27370,27372,27380,27381,27385,27386,27390,27391,27392,27393,27394,27395,27396,27397,27400,27403,27405,27407,27409,27412,27415,27416,27418,27420,27422,27424,27425,27427,27428,27429,27430,27435,27437,27438,27440,27441,27442,27443,27445,27446,27447,27448,27450,27454,27455,27457,27465,27466,27468,27470,27472,27475,27477,27479,27485,27486,27487,27488,27495,27496,27497,27498,27499,27500,27501,27502,27503,27506,27507,27508,27509,27510,27511,27513,27514,27516,27517,27519,27520,27524,27530,27532,27535,27536,27538,27540,27550,27552,27556,27557,27558,27560,27562,27566,27570,27580,27590,27591,27592,27594,27596,27598,27599,27600,27601,27602,27603,27604,27605,27606,27607,27610,27612,27613,27614,27615,27616,27618,27619,27620,27625,27626,27630,27632,27634,27635,27637,27638,27640,27641,27645,27646,27647,27648,27650,27652,27654,27656,27658,27659,27664,27665,27675,27676,27680,27681,27685,27686,27687,27690,27691,27692,27695,27696,27698,27700,27702,27703,27704,27705,27707,27709,27712,27715,27720,27722,27724,27725,27726,27727,27730,27732,27734,27740,27742,27745,27750,27752,27756,27758,27759,27760,27762,27766,27767,27768,27769,27780,27781,27784,27786,27788,27792,27808,27810,27814,27816,27818,27822,27823,27824,27825,27826,27827,27828,27829,27830,27831,27832,27840,27842,27846,27848,27860,27870,27871,27880,27881,27882,27884,27886,27888,27889,27892,27893,27894,27899,28001,28002,28003,28005,28008,28010,28011,28020,28022,28024,28035,28039,28041,28043,28045,28046,28047,28050,28052,28054,28055,28060,28062,28070,28072,28080,28086,28088,28090,28092,28100,28102,28103,28104,28106,28107,28108,28110,28111,28112,28113,28114,28116,28118,28119,28120,28122,28124,28126,28130,28140,28150,28153,28160,28171,28173,28175,28190,28192,28193,28200,28202,28208,28210,28220,28222,28225,28226,28230,28232,28234,28238,28240,28250,28260,28261,28262,28264,28270,28272,28280,28285,28286,28288,28289,28290,28292,28293,28294,28296,28297,28298,28299,28300,28302,28304,28305,28306,28307,28308,28309,28310,28312,28313,28315,28320,28322,28340,28341,28344,28345,28360,28400,28405,28406,28415,28420,28430,28435,28436,28445,28446,28450,28455,28456,28465,28470,28475,28476,28485,28490,28495,28496,28505,28510,28515,28525,28530,28531,28540,28545,28546,28555,28570,28575,28576,28585,28600,28605,28606,28615,28630,28635,28636,28645,28660,28665,28666,28675,28705,28715,28725,28730,28735,28737,28740,28750,28755,28760,28800,28805,28810,28820,28825,28890,28899,29000,29010,29015,29020,29025,29035,29040,29044,29046,29049,29055,29058,29065,29075,29085,29086,29105,29125,29126,29130,29131,29200,29240,29260,29280,29305,29325,29345,29355,29358,29365,29405,29425,29435,29440,29445,29450,29505,29515,29520,29530,29540,29550,29580,29581,29582,29583,29584,29700,29705,29710,29715,29720,29730,29740,29750,29799,29800,29804,29805,29806,29807,29819,29820,29821,29822,29823,29824,29825,29826,29827,29828,29830,29834,29835,29836,29837,29838,29840,29843,29844,29845,29846,29847,29848,29850,29851,29855,29856,29860,29861,29862,29863,29866,29867,29868,29870,29871,29873,29874,29875,29876,29877,29879,29880,29881,29882,29883,29884,29885,29886,29887,29888,29889,29891,29892,29893,29894,29895,29897,29898,29899,29900,29901,29902,29904,29905,29906,29907,29914,29915,29916,29999,30000,30020,30100,30110,30115,30117,30118,30120,30124,30125,30130,30140,30150,30160,30200,30210,30220,30300,30310,30320,30400,30410,30420,30430,30435,30450,30460,30462,30465,30520,30540,30545,30560,30580,30600,30620,30630,30801,30802,30901,30903,30905,30906,30915,30920,30930,30999,31000,31002,31020,31030,31032,31040,31050,31051,31070,31075,31080,31081,31084,31085,31086,31087,31090,31200,31201,31205,31225,31230,31231,31233,31235,31237,31238,31239,31240,31254,31255,31256,31267,31276,31287,31288,31290,31291,31292,31293,31294,31295,31296,31297,31299,31300,31320,31360,31365,31367,31368,31370,31375,31380,31382,31390,31395,31400,31420,31500,31502,31505,31510,31511,31512,31513,31515,31520,31525,31526,31527,31528,31529,31530,31531,31535,31536,31540,31541,31545,31546,31560,31561,31570,31571,31575,31576,31577,31578,31579,31580,31582,31584,31587,31588,31590,31595,31599,31600,31601,31603,31605,31610,31611,31612,31613,31614,31615,31620,31622,31623,31624,31625,31626,31627,31628,31629,31630,31631,31632,31633,31634,31635,31636,31637,31638,31640,31641,31643,31645,31646,31647,31648,31649,31651,31660,31661,31717,31720,31725,31730,31750,31755,31760,31766,31770,31775,31780,31781,31785,31786,31800,31805,31820,31825,31830,31899,32035,32036,32095,32096,32097,32098,32100,32110,32120,32124,32140,32141,32150,32151,32160,32200,32215,32220,32225,32310,32320,32400,32402,32405,32440,32442,32445,32480,32482,32484,32486,32488,32491,32500,32501,32503,32504,32505,32506,32507,32540,32550,32551,32552,32553,32554,32555,32556,32557,32560,32561,32562,32601,32602,32603,32604,32605,32606,32607,32608,32609,32650,32651,32652,32653,32654,32655,32656,32657,32658,32659,32660,32661,32662,32663,32664,32665,32666,32667,32668,32669,32670,32671,32672,32673,32674,32701,32800,32810,32815,32820,32850,32851,32852,32853,32854,32855,32856,32900,32905,32906,32940,32960,32997,32998,32999,33010,33011,33015,33020,33025,33030,33031,33050,33120,33130,33140,33141,33202,33203,33206,33207,33208,33210,33211,33212,33213,33214,33215,33216,33217,33218,33220,33221,33222,33223,33224,33225,33226,33227,33228,33229,33230,33231,33233,33234,33235,33236,33237,33238,33240,33241,33243,33244,33249,33250,33251,33254,33255,33256,33257,33258,33259,33261,33262,33263,33264,33265,33266,33282,33284,33300,33305,33310,33315,33320,33321,33322,33330,33332,33335,33361,33362,33363,33364,33365,33366,33367,33368,33369,33400,33401,33403,33404,33405,33406,33410,33411,33412,33413,33414,33415,33416,33417,33420,33422,33425,33426,33427,33430,33460,33463,33464,33465,33468,33470,33471,33472,33474,33475,33476,33478,33496,33500,33501,33502,33503,33504,33505,33506,33507,33508,33510,33511,33512,33513,33514,33516,33517,33518,33519,33521,33522,33523,33530,33533,33534,33535,33536,33542,33545,33548,33572,33600,33602,33606,33608,33610,33611,33612,33615,33617,33619,33620,33621,33622,33641,33645,33647,33660,33665,33670,33675,33676,33677,33681,33684,33688,33690,33692,33694,33697,33702,33710,33720,33722,33724,33726,33730,33732,33735,33736,33737,33750,33755,33762,33764,33766,33767,33768,33770,33771,33774,33775,33776,33777,33778,33779,33780,33781,33782,33783,33786,33788,33800,33802,33803,33813,33814,33820,33822,33824,33840,33845,33851,33852,33853,33860,33861,33863,33864,33870,33875,33877,33880,33881,33883,33884,33886,33889,33891,33910,33915,33916,33917,33920,33922,33924,33925,33926,33930,33933,33935,33940,33944,33945,33960,33961,33967,33968,33970,33971,33973,33974,33975,33976,33977,33978,33979,33980,33981,33982,33983,33990,33991,33992,33993,33999,34001,34051,34101,34111,34151,34201,34203,34401,34421,34451,34471,34490,34501,34502,34510,34520,34530,34800,34802,34803,34804,34805,34806,34808,34812,34813,34820,34825,34826,34830,34831,34832,34833,34834,34841,34842,34843,34844,34845,34846,34847,34848,34900,35001,35002,35005,35011,35013,35021,35022,35045,35081,35082,35091,35092,35102,35103,35111,35112,35121,35122,35131,35132,35141,35142,35151,35152,35180,35182,35184,35188,35189,35190,35201,35206,35207,35211,35216,35221,35226,35231,35236,35241,35246,35251,35256,35261,35266,35271,35276,35281,35286,35301,35302,35303,35304,35305,35306,35311,35321,35331,35341,35351,35355,35361,35363,35371,35372,35390,35400,35450,35452,35454,35456,35458,35459,35460,35470,35471,35472,35473,35474,35475,35476,35480,35481,35482,35483,35484,35485,35490,35491,35492,35493,35494,35495,35500,35501,35506,35508,35509,35510,35511,35512,35515,35516,35518,35521,35522,35523,35525,35526,35531,35533,35535,35536,35537,35538,35539,35540,35548,35549,35551,35556,35558,35560,35563,35565,35566,35570,35571,35572,35583,35585,35587,35600,35601,35606,35612,35616,35621,35623,35626,35631,35632,35633,35634,35636,35637,35638,35642,35645,35646,35647,35650,35651,35654,35656,35661,35663,35665,35666,35671,35681,35682,35683,35685,35686,35691,35693,35694,35695,35697,35700,35701,35721,35741,35761,35800,35820,35840,35860,35870,35875,35876,35879,35881,35883,35884,35901,35903,35905,35907,36000,36002,36005,36010,36011,36012,36013,36014,36015,36100,36120,36140,36147,36148,36160,36200,36215,36216,36217,36218,36221,36222,36223,36224,36225,36226,36227,36228,36245,36246,36247,36248,36251,36252,36253,36254,36260,36261,36262,36299,36400,36405,36406,36410,36420,36425,36430,36440,36450,36455,36460,36468,36469,36470,36471,36475,36476,36478,36479,36481,36500,36510,36511,36512,36513,36514,36515,36516,36522,36555,36556,36557,36558,36560,36561,36563,36565,36566,36568,36569,36570,36571,36575,36576,36578,36580,36581,36582,36583,36584,36585,36589,36590,36591,36592,36593,36595,36596,36597,36598,36600,36620,36625,36640,36660,36680,36800,36810,36815,36818,36819,36820,36821,36822,36823,36825,36830,36831,36832,36833,36835,36838,36860,36861,36870,37140,37145,37160,37180,37181,37182,37183,37184,37185,37186,37187,37188,37191,37192,37193,37195,37197,37200,37202,37211,37212,37213,37214,37215,37216,37217,37220,37221,37222,37223,37224,37225,37226,37227,37228,37229,37230,37231,37232,37233,37234,37235,37236,37237,37238,37239,37241,37242,37243,37244,37250,37251,37500,37501,37565,37600,37605,37606,37607,37609,37615,37616,37617,37618,37619,37620,37650,37660,37700,37718,37722,37735,37760,37761,37765,37766,37780,37785,37788,37790,37799,38100,38101,38102,38115,38120,38129,38200,38204,38205,38206,38207,38208,38209,38210,38211,38212,38213,38214,38215,38220,38221,38230,38232,38240,38241,38242,38243,38300,38305,38308,38380,38381,38382,38500,38505,38510,38520,38525,38530,38542,38550,38555,38562,38564,38570,38571,38572,38589,38700,38720,38724,38740,38745,38746,38747,38760,38765,38770,38780,38790,38792,38794,38900,38999,39000,39010,39200,39220,39400,39499,39501,39502,39503,39520,39530,39531,39540,39541,39545,39560,39561,39599,40490,40500,40510,40520,40525,40527,40530,40650,40652,40654,40700,40701,40702,40720,40761,40799,40800,40801,40804,40805,40806,40808,40810,40812,40814,40816,40818,40819,40820,40830,40831,40840,40842,40843,40844,40845,40899,41000,41005,41006,41007,41008,41009,41010,41015,41016,41017,41018,41019,41100,41105,41108,41110,41112,41113,41114,41115,41116,41120,41130,41135,41140,41145,41150,41153,41155,41250,41251,41252,41500,41510,41512,41520,41530,41599,41800,41805,41806,41820,41821,41822,41823,41825,41826,41827,41828,41830,41850,41870,41872,41874,41899,42000,42100,42104,42106,42107,42120,42140,42145,42160,42180,42182,42200,42205,42210,42215,42220,42225,42226,42227,42235,42260,42280,42281,42299,42300,42305,42310,42320,42330,42335,42340,42400,42405,42408,42409,42410,42415,42420,42425,42426,42440,42450,42500,42505,42507,42508,42509,42510,42550,42600,42650,42660,42665,42699,42700,42720,42725,42800,42804,42806,42808,42809,42810,42815,42820,42821,42825,42826,42830,42831,42835,42836,42842,42844,42845,42860,42870,42890,42892,42894,42900,42950,42953,42955,42960,42961,42962,42970,42971,42972,42999,43020,43030,43045,43100,43101,43107,43108,43112,43113,43116,43117,43118,43121,43122,43123,43124,43130,43135,43191,43192,43193,43194,43195,43196,43197,43198,43200,43201,43202,43204,43205,43206,43211,43212,43213,43214,43215,43216,43217,43220,43226,43227,43229,43231,43232,43233,43235,43236,43237,43238,43239,43240,43241,43242,43243,43244,43245,43246,43247,43248,43249,43250,43251,43252,43253,43254,43255,43257,43259,43260,43261,43262,43263,43264,43265,43266,43270,43273,43274,43275,43276,43277,43278,43279,43280,43281,43282,43283,43289,43300,43305,43310,43312,43313,43314,43320,43324,43325,43326,43327,43328,43330,43331,43332,43333,43334,43335,43336,43337,43338,43340,43341,43350,43351,43352,43360,43361,43400,43401,43405,43410,43415,43420,43425,43450,43453,43460,43496,43499,43500,43501,43502,43510,43520,43600,43605,43610,43611,43620,43621,43622,43631,43632,43633,43634,43635,43640,43641,43644,43645,43647,43648,43651,43652,43653,43659,43752,43753,43754,43755,43756,43757,43760,43761,43770,43771,43772,43773,43774,43775,43800,43810,43820,43825,43830,43831,43832,43840,43842,43843,43845,43846,43847,43848,43850,43855,43860,43865,43870,43880,43881,43882,43886,43887,43888,43999,44005,44010,44015,44020,44021,44025,44050,44055,44100,44110,44111,44120,44121,44125,44126,44127,44128,44130,44132,44133,44135,44136,44137,44139,44140,44141,44143,44144,44145,44146,44147,44150,44151,44155,44156,44157,44158,44160,44180,44186,44187,44188,44202,44203,44204,44205,44206,44207,44208,44210,44211,44212,44213,44227,44238,44300,44310,44312,44314,44316,44320,44322,44340,44345,44346,44360,44361,44363,44364,44365,44366,44369,44370,44372,44373,44376,44377,44378,44379,44380,44382,44383,44385,44386,44388,44389,44390,44391,44392,44393,44394,44397,44500,44602,44603,44604,44605,44615,44620,44625,44626,44640,44650,44660,44661,44680,44700,44701,44715,44720,44721,44799,44800,44820,44850,44899,44900,44950,44955,44960,44970,44979,45000,45005,45020,45100,45108,45110,45111,45112,45113,45114,45116,45119,45120,45121,45123,45126,45130,45135,45136,45150,45160,45171,45172,45190,45300,45303,45305,45307,45308,45309,45315,45317,45320,45321,45327,45330,45331,45332,45333,45334,45335,45337,45338,45339,45340,45341,45342,45345,45355,45378,45379,45380,45381,45382,45383,45384,45385,45386,45387,45391,45392,45395,45397,45400,45402,45499,45500,45505,45520,45540,45541,45550,45560,45562,45563,45800,45805,45820,45825,45900,45905,45910,45915,45990,45999,46020,46030,46040,46045,46050,46060,46070,46080,46083,46200,46220,46221,46230,46250,46255,46257,46258,46260,46261,46262,46270,46275,46280,46285,46288,46320,46500,46505,46600,46604,46606,46608,46610,46611,46612,46614,46615,46700,46705,46706,46707,46710,46712,46715,46716,46730,46735,46740,46742,46744,46746,46748,46750,46751,46753,46754,46760,46761,46762,46900,46910,46916,46917,46922,46924,46930,46940,46942,46945,46946,46947,46999,47000,47001,47010,47015,47100,47120,47122,47125,47130,47133,47135,47136,47140,47141,47142,47143,47144,47145,47146,47147,47300,47350,47360,47361,47362,47370,47371,47379,47380,47381,47382,47399,47400,47420,47425,47460,47480,47490,47500,47505,47510,47511,47525,47530,47550,47552,47553,47554,47555,47556,47560,47561,47562,47563,47564,47570,47579,47600,47605,47610,47612,47620,47630,47700,47701,47711,47712,47715,47720,47721,47740,47741,47760,47765,47780,47785,47800,47801,47802,47900,47999,48000,48001,48020,48100,48102,48105,48120,48140,48145,48146,48148,48150,48152,48153,48154,48155,48160,48400,48500,48510,48520,48540,48545,48547,48548,48550,48551,48552,48554,48556,48999,49000,49002,49010,49020,49040,49060,49062,49080,49081,49082,49083,49084,49180,49203,49204,49205,49215,49220,49250,49255,49320,49321,49322,49323,49324,49325,49326,49327,49329,49400,49402,49405,49406,49407,49411,49412,49418,49419,49420,49421,49422,49423,49424,49425,49426,49427,49428,49429,49435,49436,49440,49441,49442,49446,49450,49451,49452,49460,49465,49491,49492,49495,49496,49500,49501,49505,49507,49520,49521,49525,49540,49550,49553,49555,49557,49560,49561,49565,49566,49568,49570,49572,49580,49582,49585,49587,49590,49600,49605,49606,49610,49611,49650,49651,49652,49653,49654,49655,49656,49657,49659,49900,49904,49905,49906,49999,50010,50020,50040,50045,50060,50065,50070,50075,50080,50081,50100,50120,50125,50130,50135,50200,50205,50220,50225,50230,50234,50236,50240,50250,50280,50290,50300,50320,50323,50325,50327,50328,50329,50340,50360,50365,50370,50380,50382,50384,50385,50386,50387,50389,50390,50391,50392,50393,50394,50395,50396,50398,50400,50405,50500,50520,50525,50526,50540,50541,50542,50543,50544,50545,50546,50547,50548,50549,50551,50553,50555,50557,50561,50562,50570,50572,50574,50575,50576,50580,50590,50592,50593,50600,50605,50610,50620,50630,50650,50660,50684,50686,50688,50690,50700,50715,50722,50725,50727,50728,50740,50750,50760,50770,50780,50782,50783,50785,50800,50810,50815,50820,50825,50830,50840,50845,50860,50900,50920,50930,50940,50945,50947,50948,50949,50951,50953,50955,50957,50961,50970,50972,50974,50976,50980,51020,51030,51040,51045,51050,51060,51065,51080,51100,51101,51102,51500,51520,51525,51530,51535,51550,51555,51565,51570,51575,51580,51585,51590,51595,51596,51597,51600,51605,51610,51700,51701,51702,51703,51705,51710,51715,51720,51725,51726,51727,51728,51729,51736,51741,51784,51785,51792,51797,51798,51800,51820,51840,51841,51845,51860,51865,51880,51900,51920,51925,51940,51960,51980,51990,51992,51999,52000,52001,52005,52007,52010,52204,52214,52224,52234,52235,52240,52250,52260,52265,52270,52275,52276,52277,52281,52282,52283,52285,52287,52290,52300,52301,52305,52310,52315,52317,52318,52320,52325,52327,52330,52332,52334,52341,52342,52343,52344,52345,52346,52351,52352,52353,52354,52355,52356,52400,52402,52450,52500,52601,52630,52640,52647,52648,52649,52700,53000,53010,53020,53025,53040,53060,53080,53085,53200,53210,53215,53220,53230,53235,53240,53250,53260,53265,53270,53275,53400,53405,53410,53415,53420,53425,53430,53431,53440,53442,53444,53445,53446,53447,53448,53449,53450,53460,53500,53502,53505,53510,53515,53520,53600,53601,53605,53620,53621,53660,53661,53665,53850,53852,53855,53860,53899,54000,54001,54015,54050,54055,54056,54057,54060,54065,54100,54105,54110,54111,54112,54115,54120,54125,54130,54135,54150,54160,54161,54162,54163,54164,54200,54205,54220,54230,54231,54235,54240,54250,54300,54304,54308,54312,54316,54318,54322,54324,54326,54328,54332,54336,54340,54344,54348,54352,54360,54380,54385,54390,54400,54401,54405,54406,54408,54410,54411,54415,54416,54417,54420,54430,54435,54440,54450,54500,54505,54512,54520,54522,54530,54535,54550,54560,54600,54620,54640,54650,54660,54670,54680,54690,54692,54699,54700,54800,54830,54840,54860,54861,54865,54900,54901,55000,55040,55041,55060,55100,55110,55120,55150,55175,55180,55200,55250,55300,55400,55450,55500,55520,55530,55535,55540,55550,55559,55600,55605,55650,55680,55700,55705,55706,55720,55725,55801,55810,55812,55815,55821,55831,55840,55842,55845,55860,55862,55865,55866,55870,55873,55875,55876,55899,55920,55970,55980,56405,56420,56440,56441,56442,56501,56515,56605,56606,56620,56625,56630,56631,56632,56633,56634,56637,56640,56700,56740,56800,56805,56810,56820,56821,57000,57010,57020,57022,57023,57061,57065,57100,57105,57106,57107,57109,57110,57111,57112,57120,57130,57135,57150,57155,57156,57160,57170,57180,57200,57210,57220,57230,57240,57250,57260,57265,57267,57268,57270,57280,57282,57283,57284,57285,57287,57288,57289,57291,57292,57295,57296,57300,57305,57307,57308,57310,57311,57320,57330,57335,57400,57410,57415,57420,57421,57423,57425,57426,57452,57454,57455,57456,57460,57461,57500,57505,57510,57511,57513,57520,57522,57530,57531,57540,57545,57550,57555,57556,57558,57700,57720,57800,58100,58110,58120,58140,58145,58146,58150,58152,58180,58200,58210,58240,58260,58262,58263,58267,58270,58275,58280,58285,58290,58291,58292,58293,58294,58300,58301,58321,58322,58323,58340,58345,58346,58350,58353,58356,58400,58410,58520,58540,58541,58542,58543,58544,58545,58546,58548,58550,58552,58553,58554,58555,58558,58559,58560,58561,58562,58563,58565,58570,58571,58572,58573,58578,58579,58600,58605,58611,58615,58660,58661,58662,58670,58671,58672,58673,58679,58700,58720,58740,58750,58752,58760,58770,58800,58805,58820,58822,58825,58900,58920,58925,58940,58943,58950,58951,58952,58953,58954,58956,58957,58958,58960,58970,58974,58976,58999,59000,59001,59012,59015,59020,59025,59030,59050,59051,59070,59072,59074,59076,59100,59120,59121,59130,59135,59136,59140,59150,59151,59160,59200,59300,59320,59325,59350,59400,59409,59410,59412,59414,59425,59426,59430,59510,59514,59515,59525,59610,59612,59614,59618,59620,59622,59820,59821,59830,59870,59871,59897,59898,59899,60000,60100,60200,60210,60212,60220,60225,60240,60252,60254,60260,60270,60271,60280,60281,60300,60500,60502,60505,60512,60520,60521,60522,60540,60545,60600,60605,60650,60659,60699,61000,61001,61020,61026,61050,61055,61070,61105,61107,61108,61120,61140,61150,61151,61154,61156,61210,61215,61250,61253,61304,61305,61312,61313,61314,61315,61316,61320,61321,61322,61323,61330,61332,61333,61334,61340,61343,61345,61440,61450,61458,61460,61470,61480,61490,61500,61501,61510,61512,61514,61516,61517,61518,61519,61520,61521,61522,61524,61526,61530,61531,61533,61534,61535,61536,61537,61538,61539,61540,61541,61542,61543,61544,61545,61546,61548,61550,61552,61556,61557,61558,61559,61563,61564,61566,61567,61570,61571,61575,61576,61580,61581,61582,61583,61584,61585,61586,61590,61591,61592,61595,61596,61597,61598,61600,61601,61605,61606,61607,61608,61609,61610,61611,61612,61613,61615,61616,61618,61619,61623,61624,61626,61630,61635,61640,61641,61642,61680,61682,61684,61686,61690,61692,61697,61698,61700,61702,61703,61705,61708,61710,61711,61720,61735,61750,61751,61760,61770,61781,61782,61783,61790,61791,61795,61796,61797,61798,61799,61800,61850,61860,61863,61864,61867,61868,61870,61875,61880,61885,61886,61888,62000,62005,62010,62100,62115,62116,62117,62120,62121,62140,62141,62142,62143,62145,62146,62147,62148,62160,62161,62162,62163,62164,62165,62180,62190,62192,62194,62200,62201,62220,62223,62225,62230,62252,62256,62258,62263,62264,62267,62268,62269,62270,62272,62273,62280,62281,62282,62284,62287,62290,62291,62292,62294,62310,62311,62318,62319,62350,62351,62355,62360,62361,62362,62365,62367,62368,62369,62370,63001,63003,63005,63011,63012,63015,63016,63017,63020,63030,63035,63040,63042,63043,63044,63045,63046,63047,63048,63050,63051,63055,63056,63057,63064,63066,63075,63076,63077,63078,63081,63082,63085,63086,63087,63088,63090,63091,63101,63102,63103,63170,63172,63173,63180,63182,63185,63190,63191,63194,63195,63196,63197,63198,63199,63200,63250,63251,63252,63265,63266,63267,63268,63270,63271,63272,63273,63275,63276,63277,63278,63280,63281,63282,63283,63285,63286,63287,63290,63295,63300,63301,63302,63303,63304,63305,63306,63307,63308,63600,63610,63615,63620,63621,63650,63655,63661,63662,63663,63664,63685,63688,63700,63702,63704,63706,63707,63709,63710,63740,63741,63744,63746,64400,64402,64405,64408,64410,64412,64413,64415,64416,64417,64418,64420,64421,64425,64430,64435,64445,64446,64447,64448,64449,64450,64455,64479,64480,64483,64484,64490,64491,64492,64493,64494,64495,64505,64508,64510,64517,64520,64530,64550,64553,64555,64560,64561,64565,64566,64568,64569,64570,64573,64575,64577,64580,64581,64585,64590,64595,64600,64605,64610,64611,64612,64615,64616,64617,64620,64622,64623,64626,64627,64630,64632,64633,64634,64635,64636,64640,64642,64643,64644,64645,64646,64647,64650,64653,64680,64681,64702,64704,64708,64712,64713,64714,64716,64718,64719,64721,64722,64726,64727,64732,64734,64736,64738,64740,64742,64744,64746,64752,64755,64760,64761,64763,64766,64771,64772,64774,64776,64778,64782,64783,64784,64786,64787,64788,64790,64792,64795,64802,64804,64809,64818,64820,64821,64822,64823,64831,64832,64834,64835,64836,64837,64840,64856,64857,64858,64859,64861,64862,64864,64865,64866,64868,64870,64872,64874,64876,64885,64886,64890,64891,64892,64893,64895,64896,64897,64898,64901,64902,64905,64907,64910,64911,64999,65091,65093,65101,65103,65105,65110,65112,65114,65125,65130,65135,65140,65150,65155,65175,65205,65210,65220,65222,65235,65260,65265,65270,65272,65273,65275,65280,65285,65286,65290,65400,65410,65420,65426,65430,65435,65436,65450,65600,65710,65730,65750,65755,65756,65757,65760,65765,65767,65770,65771,65772,65775,65778,65779,65780,65781,65782,65800,65810,65815,65820,65850,65855,65860,65865,65870,65875,65880,65900,65920,65930,66020,66030,66130,66150,66155,66160,66165,66170,66172,66174,66175,66180,66183,66185,66220,66225,66250,66500,66505,66600,66605,66625,66630,66635,66680,66682,66700,66710,66711,66720,66740,66761,66762,66770,66820,66821,66825,66830,66840,66850,66852,66920,66930,66940,66982,66983,66984,66985,66986,66990,66999,67005,67010,67015,67025,67027,67028,67030,67031,67036,67039,67040,67041,67042,67043,67101,67105,67107,67108,67110,67112,67113,67115,67120,67121,67141,67145,67208,67210,67218,67220,67221,67225,67227,67228,67229,67250,67255,67299,67311,67312,67314,67316,67318,67320,67331,67332,67334,67335,67340,67343,67345,67346,67399,67400,67405,67412,67413,67414,67415,67420,67430,67440,67445,67450,67500,67505,67515,67550,67560,67570,67599,67700,67710,67715,67800,67801,67805,67808,67810,67820,67825,67830,67835,67840,67850,67875,67880,67882,67900,67901,67902,67903,67904,67906,67908,67909,67911,67912,67914,67915,67916,67917,67921,67922,67923,67924,67930,67935,67938,67950,67961,67966,67971,67973,67974,67975,67999,68020,68040,68100,68110,68115,68130,68135,68200,68320,68325,68326,68328,68330,68335,68340,68360,68362,68371,68399,68400,68420,68440,68500,68505,68510,68520,68525,68530,68540,68550,68700,68705,68720,68745,68750,68760,68761,68770,68801,68810,68811,68815,68816,68840,68850,68899,69000,69005,69020,69090,69100,69105,69110,69120,69140,69145,69150,69155,69200,69205,69210,69220,69222,69300,69310,69320,69399,69400,69401,69405,69420,69421,69424,69433,69436,69440,69450,69501,69502,69505,69511,69530,69535,69540,69550,69552,69554,69601,69602,69603,69604,69605,69610,69620,69631,69632,69633,69635,69636,69637,69641,69642,69643,69644,69645,69646,69650,69660,69661,69662,69666,69667,69670,69676,69700,69710,69711,69714,69715,69717,69718,69720,69725,69740,69745,69799,69801,69802,69805,69806,69820,69840,69905,69910,69915,69930,69949,69950,69955,69960,69970,69979,69990,77261,77262,77263,77280,77285,77290,77293,77295,77299,77300,77301,77305,77310,77315,77321,77326,77327,77328,77331,77332,77333,77334,77336,77338,77370,77371,77372,77373,77399,77401,77402,77403,77404,77406,77407,77408,77409,77411,77412,77413,77414,77416,77417,77418,77421,77422,77423,77424,77425,77427,77431,77432,77435,77469,77470,77499,77520,77522,77523,77525,77600,77605,77610,77615,77620,77750,77761,77762,77763,77776,77777,77778,77785,77786,77787,77789,77790,77799,79005,79101,79200,79300,79403,79440,79445,79999,90791,90792,90832,90833,90834,90836,90837,90838,90839,90840,90845,90846,90847,90849,90853,90863,90865,90867,90868,90869,90870,90875,90876,90880,90935,90937,90940,90945,90947,90951,90952,90953,90954,90955,90956,90957,90958,90959,90960,90961,90962,90963,90964,90965,90966,90967,90968,90969,90970,90989,90993,90997,90999,91000,91123,92002,92004,92012,92014,92018,92019,92225,92226,92227,92228,92502,92504,92507,92508,92521,92522,92523,92524,92526,92550,92551,92552,92553,92555,92556,92557,92558,92559,92560,92561,92562,92563,92564,92565,92567,92568,92570,92571,92572,92575,92576,92577,92579,92582,92583,92584,92585,92586,92587,92588,92590,92591,92592,92593,92594,92595,92601,92602,92603,92604,92605,92606,92607,92608,92609,92618,92620,92621,92625,92626,92627,92630,92633,92640,92700,92920,92921,92924,92925,92928,92929,92933,92934,92937,92938,92941,92943,92944,92950,92953,92960,92961,92970,92971,92973,92974,92975,92977,92978,92979,92986,92987,92990,92992,92993,92997,92998,93303,93304,93306,93307,93308,93312,93313,93314,93315,93316,93317,93318,93320,93321,93325,93350,93351,93352,93451,93452,93453,93454,93455,93456,93457,93458,93459,93460,93461,93462,93463,93464,93501,93503,93505,93508,93510,93511,93514,93524,93526,93527,93528,93529,93530,93531,93532,93533,93539,93540,93541,93542,93543,93544,93545,93555,93556,93580,93581,93582,93583,93653,93654,93655,93656,93657,93797,93798,94002,94003,94004,94005,94610,94620,94640,94642,94644,94645,94660,94662,94664,94667,94668,94669,95115,95117,95120,95125,95130,95131,95132,95133,95134,95144,95145,95146,95147,95148,95149,95165,95170,95180,95199,95991,95992,96004,96020,96040,96101,96102,96103,96105,96110,96111,96116,96118,96119,96120,96125,96150,96151,96152,96153,96154,96155,96360,96361,96365,96366,96367,96368,96369,96370,96371,96372,96373,96374,96375,96376,96379,96401,96402,96405,96406,96409,96411,96413,96415,96416,96417,96420,96422,96423,96425,96440,96445,96446,96450,96521,96522,96523,96542,96549,96567,96570,96571,96900,96910,96912,96913,96920,96921,96922,96999,97001,97002,97003,97004,97005,97006,97010,97012,97014,97016,97018,97022,97024,97026,97028,97032,97033,97034,97035,97036,97039,97110,97112,97113,97116,97124,97139,97140,97150,97530,97532,97533,97535,97537,97542,97545,97546,97597,97598,97602,97605,97606,97610,97750,97755,97760,97761,97762,97799,97802,97803,97804,98925,98926,98927,98928,98929,98940,98941,98942,98943,99024,99050,99051,99053,99056,99058,99060,99100,99116,99135,99140,99143,99144,99145,99148,99149,99150,99175,99183,99195,99201,99202,99203,99204,99205,99211,99212,99213,99214,99215,99218,99219,99220,99221,99222,99223,99224,99225,99226,99231,99232,99233,99234,99235,99236,99241,99242,99243,99244,99245,99251,99252,99253,99254,99255,99281,99282,99283,99284,99285,99288,99291,99292,99304,99305,99306,99307,99308,99309,99310,99318,99324,99325,99326,99327,99328,99334,99335,99336,99337,99341,99342,99343,99344,99345,99347,99348,99349,99350,99354,99355,99356,99357,99358,99359,99363,99364,99381,99382,99383,99384,99385,99386,99387,99391,99392,99393,99394,99395,99396,99397,99455,99456,99460,99461,99462,99463,99464,99465,99466,99467,99468,99469,99471,99472,99475,99476,99477,99478,99479,99480,99481,99482,99495,99496,99499,99500,99501,99502,99503,99504,99505,99506,99507,99509,99510,99511,99512,99600,99601,99602,G0129,G0151,G0152,G0153,G0155,G0159,G0160,G0161,G0173,G0186,G0245,G0246,G0247,G0248,G0249,G0250,G0251,G0257,G0259,G0260,G0268,G0269,G0289,G0339,G0340,G0341,G0342,G0343,G0378,G0379,G0380,G0381,G0382,G0383,G0384,G0402,G0406,G0407,G0408,G0409,G0412,G0413,G0414,G0415,G0425,G0426,G0427,G0428,G0429,G0438,G0439,G0440,G0441,G0443,G0447,G0448,G0455,G0456,G0457,G0458,G0459,G0460,G0463,G3001,H0007,H0008,H0009,H0010,H0011,H0012,H0013,H0014,H0015,H0016,H0020,H0050,H1000,H1001,H1002,H1003,H1004,H1005,H2033,M0064,M0075,M0076,M0100,M0300,M0301,S0201,S0220,S0221,S0260,S0265,S0273,S0274,S0310,S0353,S0354,S0390,S0400,S0610,S0612,S0613,S0620,S0621,S0630,S0800,S0810,S0812,S2053,S2054,S2060,S2065,S2066,S2067,S2068,S2070,S2079,S2080,S2083,S2095,S2102,S2103,S2107,S2112,S2115,S2117,S2118,S2120,S2150,S2152,S2202,S2205,S2206,S2207,S2208,S2209,S2225,S2230,S2235,S2300,S2325,S2340,S2341,S2342,S2344,S2348,S2350,S2351,S2360,S2361,S2400,S2401,S2402,S2403,S2404,S2405,S2409,S2411,S2900,S3000,S4013,S4014,S4028,S4989,S4993,S5522,S5523,S9034,S9128,S9129,S9131,S9145,S9152,T1024,T1025,T1026,";
            foreach (XmlNode plan in xmlRACSD.DocumentElement.ChildNodes[1].ChildNodes)
                if (plan.Name.Replace("ns1:", "") == "includedPlanCategory")
                {
                    foreach (XmlNode claim in plan.ChildNodes)
                        if (claim.Name.Replace("ns1:", "") == "includedMedicalClaimCategory")
                        {
                            strClaimID = "";
                            bRejected = false;
                            string x = claim.InnerXml.Replace(" xmlns:ns1=\"http://vo.edge.fm.cms.hhs.gov\"", "").Replace("ns1:", "");
                            XmlNamespaceManager mgr = new XmlNamespaceManager(xmlRACSD.NameTable);
                            mgr.AddNamespace("ns", "http://vo.edge.fm.cms.hhs.gov");
                            //plan.SelectNodes("ns:planIdentifier", mgr);
                            strReasonCode = "";
                            bRejected = false;

                            foreach (XmlNode node in claim.ChildNodes)
                            {
                                if (node.Name.Replace("ns1:", "") == "raEligibleIndicator" && node.InnerText == "0")
                                    bRejected = true;
                                if (node.Name.Replace("ns1:", "") == "medicalClaimIdentifier")
                                    strClaimID = node.InnerText;
                                if (strClaimID == "1430011324HC")
                                    strClaimID = "1430011324HC";
                                if (node.Name.Replace("ns1:", "") == "serviceCode")
                                    strServiceCode = node.InnerText;
                                if (node.Name.Replace("ns1:", "") == "billTypeCode")
                                    strBillTypeCode = node.InnerText;
                                if (node.Name.Replace("ns1:", "") == "reasonCode")
                                    strReasonCode = node.InnerText;
                                if (bRejected && strReasonCode == "R03")
                                {
                                    bool bFound = false;
                                    for (int i = 0; i < alServiceCodes.Count && !bFound; i++)
                                        if (((string)alServiceCodes[i]) == strServiceCode)
                                        {
                                            int temp = ((int)alCounts[i]);
                                            alCounts[i] = ++temp;
                                            bFound = true;
                                        }
                                    if (!bFound)
                                    {
                                        alServiceCodes.Add(strServiceCode);
                                        alCounts.Add(1);
                                    }
                                }
                            }
                            //if (bRejected && strReasonCode == "R01" && !strAllBillTypes.Contains(strBillTypeCode))
                            //{
                            //    if (strBillTypeCode == "131")
                            //        strBillTypeCode = "131";
                            //    strAllBillTypes += strBillTypeCode + " ";
                            //}
                            //if (!xmlRACSD.InnerText.Contains(strClaimID))  //SO IF THIS CLAIM IS *NOT* IN THE RIDE OUTPUT...
                            //    textBox1.Text += strClaimID + " " + strPaid + Environment.NewLine;
                        }
                }
            for (int i = 0; i < alServiceCodes.Count; i++)
            {
                string strValid = "?";
                if (allINVALIDscs.Contains("," + strServiceCode + ","))
                    strValid = "N";
                if (allVALIDscs.Contains("," + strServiceCode + ","))
                    strValid = "Y";

                sw.WriteLine(alServiceCodes[i] + " " + alCounts[i] + " " + strValid);
            }
            sw.Close();
        }

        private void btnDelimit_CARA_Click(object sender, EventArgs e)
        {
            string plan_insurancePlanIdentifier; //NO
            string plan_insurancePlanFileDetailTotalQuantity; //CAN BE DERIVED
            string insuredMemberIdentifier; //IN FILE, PROBABLY
            string supplementalDiagnosisDetailRecordIdentifier; //OURS
            string originalClaimIdentifier; //NEEDED?
            string detailRecordProcessedDateTime; 
            string addDeleteVoidCode;
            string originalSupplementalDetailID;
            string serviceFromDate;
            string serviceToDate;
            string diagnosisTypeCode;
            string supplementalDiagnosisCode;
            string sourceCode;

            StreamReader sr = null;
            StreamWriter sw = null;
            string thisLine;

            sw = new StreamWriter("C:\\Users\\Pat\\Documents\\CoChoice\\EDGE Server\\Inovalon - CARA\\cara delimited.txt", false);

            OpenFileDialog OpnFlDlg_SDC = new OpenFileDialog();
            OpnFlDlg_SDC.CheckFileExists = true;
            OpnFlDlg_SDC.Title = "Cara TXT file:";
            OpnFlDlg_SDC.Filter = "Text Files (*.TXT)|*.TXT";
            OpnFlDlg_SDC.DefaultExt = ".TXT";
            OpnFlDlg_SDC.AddExtension = false;
            if (OpnFlDlg_SDC.ShowDialog() != DialogResult.OK)
                return;
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                sw.WriteLine("RecordType" + '\t' + "PlanID" + '\t' + "PatientControl#"
                    + '\t' + "MID" + '\t' + "HHS_MID" + '\t' + "Last" + '\t' + "First" + '\t' + "DOB" + '\t' + "ProvID"
                    + '\t' + "Prov" + '\t' + "City" + '\t' + "From" + '\t' + "To" + '\t' + "DiagVersion" + '\t' + "DiagCode"
                    + '\t' + "DiagInd" + '\t' + "ClaimID" + '\t' + "Amount" + '\t' + "Derived" + '\t' + "SvcCodeQ" + '\t' + "SvcCode"
                    + '\t' + "PlaceOfSvc" + '\t' + "DiagSource" + '\t' + "Delete" + '\t' + "OrigPatientControlNo");

                sr = new StreamReader(OpnFlDlg_SDC.FileName);
                sr.ReadLine(); //HEADER, USELESS
                while (sr.Peek() != -1)
                {
                    thisLine = sr.ReadLine();
                    if (thisLine.StartsWith("ZZ"))
                        break;
                    if (thisLine.StartsWith("AA"))
                        continue;
                    sw.WriteLine(thisLine.Substring(0, 2).Trim() + '\t' + thisLine.Substring(2, 2).Trim() + '\t'
                        + thisLine.Substring(44, 25).Trim() + '\t' + thisLine.Substring(69, 80).Trim() + '\t'
                        + thisLine.Substring(189, 20).Trim() + '\t' + thisLine.Substring(209, 20).Trim() + '\t'
                        + thisLine.Substring(249, 10).Trim() + '\t' + thisLine.Substring(259, 20).Trim() + '\t'
                        + thisLine.Substring(289, 50).Trim() + '\t' + thisLine.Substring(339, 25).Trim() + '\t'
                        + thisLine.Substring(366, 10).Trim() + '\t' + thisLine.Substring(376, 10).Trim() + '\t'
                        + thisLine.Substring(386, 2).Trim() + '\t' + thisLine.Substring(388, 7).Trim() + '\t' + thisLine.Substring(395,1) + '\t'
                        + thisLine.Substring(396, 50).Trim() + '\t' + thisLine.Substring(446, 20).Trim() + '\t'
                        + thisLine.Substring(466, 1).Trim() + '\t' + thisLine.Substring(467, 1).Trim() + '\t'
                        + thisLine.Substring(469, 20).Trim() + '\t' + thisLine.Substring(489, 2).Trim() + '\t'
                        + thisLine.Substring(491, 3).Trim() + '\t' + thisLine.Substring(494, 1).Trim() + '\t'
                        + thisLine.Substring(495, 40).Trim());
                    //plan_insurancePlanIdentifier = thisLine.Substring();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception " + ex.Message);
            }
            finally
            {
                if (sr != null)
                    sr.Close();
                if (sw != null)
                    sw.Close();
            }
        }
    }
}
