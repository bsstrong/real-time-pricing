using System;
using System.Linq;
using com.LoanTek.Biz.Contacts.Leads.Objects;
using com.LoanTek.Master.Data.LinqDataContexts;
using com.LoanTek.Types;
using LoanTek.Schemas;
using LoanTek.Utilities;
using WebFramework;

namespace com.LoanTek.API.Leads.Clients.Models
{
    public class LegacyLead
    {
        public static Lead ConvertXmlSchema(MortgageLead dataLead)
        {
            if (dataLead == null)
                return null;

            Lead lead = new Lead();
            LoanTekAPI.SetLeadDefaults(lead);
            lead.UserID = dataLead.UserId.ToInt();

            if (string.IsNullOrEmpty(dataLead.Status)) dataLead.Status = LeadStatus.Assigned.ToString();

            LeadStatus leadStatus;
            if (Enum.TryParse(dataLead.Status, out leadStatus))
                lead.StatusID = (int) leadStatus;
            else
                lead.StatusID = (int) LeadStatus.Unassigned;

            #region Address update / cleanup

            var state = string.Empty;
            var zip = string.Empty;
            if (dataLead.Borrower != null)
            {
                if (string.IsNullOrEmpty(dataLead.Borrower?.MailState))
                {
                    if (dataLead.Property != null)
                    {
                        state = dataLead.Property?.PropertyState;
                        zip = dataLead.Property?.PropertyZip;
                    }
                }
                else
                {
                    state = dataLead.Borrower?.MailState;
                    zip = dataLead.Borrower?.MailZip;
                }
            }
            if (string.IsNullOrEmpty(state) && !string.IsNullOrEmpty(zip))
            {
                using (LeadsDataContext dc = new LeadsDataContext())
                {
                    var result = dc.LeadsGetStateAbbreviationByZipCode(zip).FirstOrDefault();

                    if (result != null)
                    {
                        state = NullSafe.NullSafeString(result.stateabbv);
                    }
                }
            }
            if (!string.IsNullOrEmpty(state))
            {
                if (dataLead.Borrower != null)
                {
                    if (string.IsNullOrEmpty(dataLead.Borrower?.MailState))
                    {
                        dataLead.Borrower.MailState = state;
                    }
                }
                if (dataLead.Property != null)
                {
                    if (string.IsNullOrEmpty(dataLead.Property?.PropertyState))
                    {
                        dataLead.Property.PropertyState = state;
                    }
                }
            }

            #endregion

            #region Name update / cleanup

            if (dataLead.Borrower != null)
            {
                if (dataLead.Borrower?.FirstName != null)
                    lead.FirstName = NullSafe.NullSafeString(dataLead.Borrower?.FirstName);
                if (dataLead.Borrower?.LastName != null)
                    lead.LastName = NullSafe.NullSafeString(dataLead.Borrower?.LastName);
                if (string.IsNullOrEmpty(dataLead.Borrower?.LastName))
                {
                    var nameList = NullSafe.NullSafeString(dataLead.Borrower?.FirstName).Split(' ').ToList();
                    switch (nameList.Count)
                    {
                        case 1:
                            lead.FirstName = nameList[0];
                            break;
                        case 2:
                            lead.FirstName = nameList[0];
                            lead.LastName = nameList[1];
                            break;
                        case 3:
                            lead.FirstName = nameList[0];
                            lead.MiddleName = nameList[1];
                            lead.LastName = nameList[2];
                            break;
                        case 4:
                            if (nameList.Count(w => w == "and") > 0)
                            {
                                lead.FirstName = $"{nameList[0]} {nameList[1]} {nameList[2]}";
                                lead.MiddleName = string.Empty;
                                lead.LastName = nameList[3];
                            }
                            else
                            {
                                lead.FirstName = nameList[0];
                                lead.MiddleName = nameList[1];
                                lead.LastName = $"{nameList[2]} {nameList[3]}";
                            }
                            break;
                    }
                }
            }

            #endregion

            if (dataLead.Borrower != null)
            {
                if (NullSafe.NullSafeString(dataLead?.Borrower?.GrossMonthlyIncome).Split('-').Length > 1)
                {
                    var income = NullSafe.NullSafeString(dataLead?.Borrower?.GrossMonthlyIncome).Split('-')[0];
                    dataLead.Borrower.GrossMonthlyIncome = income;
                }
                if (NullSafe.NullSafeString(dataLead?.Borrower?.FicoScore).Split('-').Length > 1)
                {
                    var fico = NullSafe.NullSafeString(dataLead?.Borrower?.FicoScore).Split('-')[0];
                    dataLead.Borrower.FicoScore = fico;
                }
            }

            lead.LenderId = dataLead.LenderId;
            lead.PartnerId = dataLead.PartnerId;
            lead.EmailScore = NullSafe.NullSafeString(dataLead?.EmailScore);
            lead.HasMobilePhone = NullSafe.NullSafeString(dataLead?.HasMobilePhone);
            lead.HasRecentPhoneActivity = NullSafe.NullSafeString(dataLead?.HasRecentPhoneActivity);
            lead.PhoneScore = NullSafe.NullSafeString(dataLead?.PhoneScore);
            lead.ReferralType = NullSafe.NullSafeString(dataLead?.ReferralType);
            lead.JornayaLeadId = NullSafe.NullSafeString(dataLead?.JornayaLeadId);

            lead.CountyID = 0;
            lead.AdditionalInformation = NullSafe.NullSafeString(dataLead.AdditionalInformation).Replace("Unknown", "");
            lead.Age = NullSafe.NullSafeInteger(dataLead?.Borrower?.Age, 0);
            lead.AnnualIncome = NullSafe.NullSafeInteger(dataLead?.Borrower?.GrossMonthlyIncome, 0) * 12;
            lead.Assigned = DateTime.Now;
            lead.Created = DateTime.Now;
            lead.CreditProfile = NullSafe.NullSafeString(dataLead?.Borrower?.CreditRating).Replace("Unknown", "");
            lead.DateOfBirth = NullSafe.NullSafeString(dataLead?.Borrower?.DateOfBirth).Replace("Unknown", "");
            lead.EmployerName = NullSafe.NullSafeString(dataLead?.Borrower?.EmployerName).Replace("Unknown", "");
            lead.EmploymentLength = NullSafe.NullSafeString(dataLead?.Borrower?.EmploymentLength).Replace("Unknown", "");
            lead.EmploymentLengthPeriod = NullSafe.NullSafeString(dataLead?.Borrower?.EmploymentLengthPeriod).Replace("Unknown", "");
            lead.EmploymentStatus = NullSafe.NullSafeString(dataLead?.Borrower?.EmploymentStatus).Replace("Unknown", "");
            lead.FicoScore = NullSafe.NullSafeInteger(dataLead?.Borrower?.FicoScore, 0);
            if (string.IsNullOrEmpty(lead.CreditProfile)) lead.CreditProfile = LoanTekAPI.GetCreditProfile((int)lead.FicoScore);
            lead.FirstName = StringUtilities.PropCase(StringUtilities.RemoveCharactersButKeepSpaces(lead.FirstName));
            lead.GrossMonthlyIncome = NullSafe.NullSafeInteger(dataLead?.Borrower?.GrossMonthlyIncome, 0);
            lead.HasCoBorrower = dataLead.CoBorrower != null;
            lead.IPAddress = NullSafe.NullSafeString(dataLead.IPAddress);
            if (lead.IPAddress.Length > 24)
                lead.IPAddress = lead.IPAddress.Substring(0, 24);
            lead.IsDeleted = false;
            lead.IsVerified = true;
            lead.LastName = StringUtilities.PropCase(StringUtilities.RemoveCharactersButKeepSpaces(lead.LastName));
            lead.LiquidAssetsValue = NullSafe.NullSafeInteger(dataLead?.Borrower?.LiquidAssetsValue, 0);
            lead.MailAddress = StringUtilities.PropCase(NullSafe.NullSafeString(dataLead?.Borrower?.MailAddress).Replace("Unknown", ""));
            lead.MailAddress2 = StringUtilities.PropCase(NullSafe.NullSafeString(dataLead?.Borrower?.MailAddress2).Replace("Unknown", ""));
            lead.MailCity = StringUtilities.PropCase(NullSafe.NullSafeString(dataLead?.Borrower?.MailCity).Replace("Unknown", ""));
            lead.MailState = LocationServices.GetStateAbbreviation(NullSafe.NullSafeString(dataLead?.Borrower?.MailState).Replace("Unknown", "")).ToUpper();
            lead.MailZip = NullSafe.NullSafeString(dataLead?.Borrower?.MailZip).Replace("Unknown", "");
            lead.MaritalStatus = NullSafe.NullSafeString(dataLead?.Borrower?.MaritalStatus).Replace("Unknown", "");
            lead.MiddleName = StringUtilities.PropCase(StringUtilities.RemoveCharacters(lead.MiddleName ?? string.Empty));
            lead.MonthlyDebt = NullSafe.NullSafeInteger(dataLead?.Borrower?.MonthlyDebt, 0);
            lead.ResidenceLength = NullSafe.NullSafeString(dataLead?.Borrower?.ResidenceLength).Replace("Unknown", "");
            lead.ResidenceLengthPeriod = NullSafe.NullSafeString(dataLead?.Borrower?.ResidenceLengthPeriod).Replace("Unknown", "");
            lead.SourceCreated = NullSafe.NullSafeDate(dataLead.SourceCreated);
            lead.SourceFilterID = LoanTekAPI.GetSourceFilterId(NullSafe.NullSafeInteger(dataLead.LeadSourceId, 0), NullSafe.NullSafeString(dataLead?.Loan?.LoanType).Replace("Unknown", ""));
            lead.SourceID = NullSafe.NullSafeInteger(dataLead.LeadSourceId, 0);
            lead.SourceLeadID = string.IsNullOrEmpty(dataLead.SourceLeadId) ? Guid.NewGuid().ToString() : NullSafe.NullSafeString(dataLead.SourceLeadId).Replace("Unknown", "");
            lead.SpouseFirstName = StringUtilities.PropCase(NullSafe.NullSafeString(dataLead?.Borrower?.SpouseFirstName).Replace("Unknown", ""));
            lead.SpouseLastName = StringUtilities.PropCase(NullSafe.NullSafeString(dataLead?.Borrower?.SpouseLastName).Replace("Unknown", ""));
            lead.SSN = StringUtilities.RemoveCharacters(NullSafe.NullSafeString(dataLead?.Borrower?.SSN).Replace("Unknown", ""));
            if (dataLead.Property != null)
                lead.StateID = NullSafe.NullSafeInteger(dataLead?.Property?.PropertyState, 0);
            //lead.StatusID = (int)LeadStatus.Assigned;
            if (dataLead.Declarations != null)
            {
                lead.Bankruptcy = NullSafe.NullSafeBoolean(dataLead.Declarations.Bankruptcy);
                lead.BankruptcyType = NullSafe.NullSafeString(dataLead.Declarations.BankruptcyType).Replace("Unknown", "");
                lead.FirstTimeBuyer = NullSafe.NullSafeBoolean(dataLead.Declarations.FirstTimeBuyer);
                lead.FHAEligible = NullSafe.NullSafeBoolean(dataLead.Declarations.FHAEligible);
                lead.Foreclosed = NullSafe.NullSafeBoolean(dataLead.Declarations.Foreclosed);
                lead.HomeOwner = NullSafe.NullSafeBoolean(dataLead.Declarations.HomeOwner);
                lead.NeedPurchaseREAgent = NullSafe.NullSafeBoolean(dataLead.Declarations.NeedPurchaseREAgent);
                lead.NeedSellingREAgent = NullSafe.NullSafeBoolean(dataLead.Declarations.NeedSellingREAgent);
                lead.RealEstateAgentName = StringUtilities.PropCase(NullSafe.NullSafeString(dataLead.Declarations.RealEstateAgentName).Replace("Unknown", ""));
                lead.RealEstateAgentPhone = StringUtilities.RemoveCharacters(NullSafe.NullSafeString(dataLead.Declarations.RealEstateAgentPhone).Replace("Unknown", ""));
                lead.VADisabled = NullSafe.NullSafeBoolean(dataLead.Declarations.VADisabled);
                lead.VAEligible = NullSafe.NullSafeBoolean(dataLead.Declarations.VAEligible);
                lead.VAFirstTime = NullSafe.NullSafeBoolean(dataLead.Declarations.VAFirstTime);
                lead.VAType = NullSafe.NullSafeString(dataLead.Declarations.VAType).Replace("Unknown", "");
                lead.Veteran = NullSafe.NullSafeBoolean(dataLead.Declarations.Veteran);
                lead.WorkingWithRealtor = NullSafe.NullSafeBoolean(dataLead.Declarations.WorkingWithRealtor);
                lead.EligibleForFHAStreamline = NullSafe.NullSafeBoolean(dataLead.Declarations.FHAStreamLineEligible);
                lead.EligibleForHARP = NullSafe.NullSafeBoolean(dataLead.Declarations.HARPEligible);
            }
            if (dataLead.CoBorrower != null)
            {
                if (NullSafe.NullSafeString(dataLead?.CoBorrower?.CoBorrowerFicoScore).Split('-').Length > 1)
                {
                    var fico = NullSafe.NullSafeString(dataLead?.CoBorrower?.CoBorrowerFicoScore).Split('-')[0];
                    dataLead.CoBorrower.CoBorrowerFicoScore = fico;
                }
                lead.CoBorrowerAddress = StringUtilities.PropCase(NullSafe.NullSafeString(dataLead?.CoBorrower?.CoBorrowerAddress).Replace("Unknown", ""));
                lead.CoBorrowerAge = NullSafe.NullSafeInteger(dataLead?.CoBorrower?.CoBorrowerAge, 0);
                lead.CoBorrowerAnnualIncome = NullSafe.NullSafeInteger(dataLead?.CoBorrower?.CoBorrowerAnnualIncome, 0);
                lead.CoBorrowerCity = StringUtilities.PropCase(NullSafe.NullSafeString(dataLead?.CoBorrower?.CoBorrowerCity).Replace("Unknown", ""));
                lead.CoBorrowerCreditProfile = NullSafe.NullSafeString(dataLead?.CoBorrower?.CreditRating).Replace("Unknown", "");
                lead.CoBorrowerDateOfBirth = NullSafe.NullSafeString(dataLead?.CoBorrower?.CoBorrowerDateOfBirth).Replace("Unknown", "");
                lead.CoBorrowerFicoScore = NullSafe.NullSafeInteger(dataLead?.CoBorrower?.CoBorrowerFicoScore, 0);
                lead.CoBorrowerFirstName = StringUtilities.PropCase(NullSafe.NullSafeString(dataLead?.CoBorrower?.CoBorrowerFirstName).Replace("Unknown", ""));
                lead.CoBorrowerLastName = StringUtilities.PropCase(NullSafe.NullSafeString(dataLead?.CoBorrower?.CoBorrowerLastName).Replace("Unknown", ""));
                lead.CoBorrowerLiquidAssetsValue = NullSafe.NullSafeInteger(dataLead?.CoBorrower?.CoBorrowerLiquidAssetsValue, 0);
                lead.CoBorrowerMonthlyIncome = NullSafe.NullSafeInteger(dataLead?.CoBorrower?.CoBorrowerMonthlyIncome, 0);
                lead.CoBorrowerSSN = StringUtilities.RemoveCharacters(NullSafe.NullSafeString(dataLead?.CoBorrower?.CoBorrowerSSN).Replace("Unknown", ""));
                lead.CoBorrowerState = NullSafe.NullSafeString(dataLead?.CoBorrower?.CoBorrowerState).Replace("Unknown", "").ToUpper();
                lead.CoBorrowerVeteran = NullSafe.NullSafeBoolean(dataLead?.CoBorrower?.CoBorrowerVeteran);
                lead.CoBorrowerZip = NullSafe.NullSafeString(dataLead?.CoBorrower?.CoBorrowerZip).Replace("Unknown", "");
            }
            if (dataLead.ContactInfo != null)
            {
                lead.BestTimeToCall = NullSafe.NullSafeString(dataLead?.ContactInfo?.BestTimeToCall).Replace("Unknown", "");
                lead.Email = NullSafe.NullSafeString(dataLead?.ContactInfo?.Email).ToLower();
                lead.Phone1 = StringUtilities.RemoveCharacters(NullSafe.NullSafeString(dataLead?.ContactInfo?.Phone1).Replace("Unknown", ""));
                lead.Phone1Type = NullSafe.NullSafeString(dataLead?.ContactInfo?.Phone1Type).Replace("Unknown", "");
                lead.Phone2 = StringUtilities.RemoveCharacters(NullSafe.NullSafeString(dataLead?.ContactInfo?.Phone2).Replace("Unknown", ""));
                lead.Phone2Type = NullSafe.NullSafeString(dataLead?.ContactInfo?.Phone2Type).Replace("Unknown", "");
                lead.Phone3 = StringUtilities.RemoveCharacters(NullSafe.NullSafeString(dataLead?.ContactInfo?.Phone3).Replace("Unknown", ""));
                lead.Phone3Type = NullSafe.NullSafeString(dataLead?.ContactInfo?.Phone3Type).Replace("Unknown", "");
                lead.Phone4 = StringUtilities.RemoveCharacters(NullSafe.NullSafeString(dataLead?.ContactInfo?.Phone4).Replace("Unknown", ""));
                lead.Phone4Type = NullSafe.NullSafeString(dataLead?.ContactInfo?.Phone4Type).Replace("Unknown", "");
            }
            if (dataLead.Loan != null)
            {
                lead.CashOut = NullSafe.NullSafeDecimal(StringUtilities.RemoveAllButNumericAndDecimal(dataLead?.Loan?.CashOut));
                if (lead.CashOut > 0)
                    lead.WantsCashOut = true;
                lead.DownPayment = NullSafe.NullSafeInteger(dataLead?.Loan?.DownPayment, 0);
                lead.FirstMortgageBalance = NullSafe.NullSafeInteger(dataLead?.Loan?.FirstMortgageBalance, 0);
                lead.FirstMortgagePayment = NullSafe.NullSafeInteger(dataLead?.Loan?.FirstMortgagePayment, 0);
                lead.FirstMortgageRate = NullSafe.NullSafeDecimal(dataLead?.Loan?.FirstMortgageRate);
                lead.FirstMortgageRateType = NullSafe.NullSafeString(dataLead?.Loan?.FirstMortgageRateType).Replace("Unknown", "");
                lead.FirstMortgageTerm = NullSafe.NullSafeInteger(dataLead?.Loan?.FirstMortgageTerm, 0);
                lead.LoanAmount = NullSafe.NullSafeInteger(dataLead?.Loan?.LoanAmount, 0);
                lead.LoanPurpose = lead.CashOut > 0 ? "Cash Out" : NullSafe.NullSafeString(dataLead?.Loan?.LoanPurpose).Replace("Unknown", "");
                lead.LoanTimeFrame = NullSafe.NullSafeString(dataLead?.Loan?.LoanTimeFrame).Replace("Item", "").Replace("Unknown", "");
                lead.LoanToValue = NullSafe.NullSafeDecimal(dataLead?.Loan?.LoanToValue);
                lead.LoanType = NullSafe.NullSafeString(dataLead?.Loan?.LoanType).Replace("Unknown", "");
                lead.SecondMortgageBalance = NullSafe.NullSafeInteger(dataLead?.Loan?.SecondMortgageBalance, 0);
                lead.SecondMortgagePayment = NullSafe.NullSafeInteger(dataLead?.Loan?.SecondMortgagePayment, 0);
                lead.SecondMortgageRate = NullSafe.NullSafeDecimal(dataLead?.Loan?.SecondMortgageRate);
                lead.SecondMortgageRateType = NullSafe.NullSafeString(dataLead?.Loan?.SecondMortgageRateType).Replace("Unknown", "");
                lead.SecondMortgageTerm = NullSafe.NullSafeInteger(dataLead?.Loan?.SecondMortgageTerm, 0);
                lead.LoanProgramType = NullSafe.NullSafeString(dataLead?.Loan?.LoanProgramType).Replace("Unknown", "");
                lead.LoanProgramName = NullSafe.NullSafeString(dataLead?.Loan?.LoanProgramName).Replace("Unknown", "");
                lead.CurrentLender = NullSafe.NullSafeString(dataLead?.Loan?.CurrentLender).Replace("\n", "").Replace("Your Current Lender", "");
                lead.DateOfLoan = NullSafe.NullSafeString(dataLead?.Loan?.DateOfLoan);
            }
            if (dataLead.Property != null)
            {
                lead.PropertyAddress = StringUtilities.PropCase(NullSafe.NullSafeString(dataLead?.Property?.PropertyAddress)).Replace("Unknown", "");
                lead.PropertyAddress2 = StringUtilities.PropCase(NullSafe.NullSafeString(dataLead?.Property?.PropertyAddress2)).Replace("Unknown", "");
                lead.PropertyCity = StringUtilities.PropCase(NullSafe.NullSafeString(dataLead?.Property?.PropertyCity)).Replace("Unknown", "");
                lead.PropertyCounty = StringUtilities.PropCase(NullSafe.NullSafeString(dataLead?.Property?.PropertyCounty)).Replace("Unknown", "");
                lead.PropertyDescription = NullSafe.NullSafeString(dataLead?.Property?.PropertyDescription).Replace("Unknown", "");
                lead.PropertyFound = NullSafe.NullSafeString(dataLead?.Property?.PropertyFound).Replace("Item", "");
                lead.PropertyNumberOfUnits = NullSafe.NullSafeInteger(dataLead?.Property?.PropertyNumberOfUnits, 0);
                lead.PropertyPurchasedYear = NullSafe.NullSafeInteger(dataLead?.Property?.PropertyPurchasedYear, 0);
                lead.PropertyPurchasePrice = NullSafe.NullSafeInteger(dataLead?.Property?.PropertyPurchasePrice, 0);
                lead.PropertyState = LocationServices.GetStateAbbreviation(NullSafe.NullSafeString(dataLead?.Property?.PropertyState).Replace("Unknown", "")).ToUpper();
                lead.PropertyType = NullSafe.NullSafeString(dataLead?.Property?.PropertyType).Replace("Unknown", "");
                lead.PropertyUsage = NullSafe.NullSafeString(dataLead?.Property?.PropertyUsage).Replace("Unknown", "");
                lead.PropertyValue = NullSafe.NullSafeInteger(dataLead?.Property?.PropertyValue, 0);
                lead.PropertyYearBuilt = NullSafe.NullSafeInteger(dataLead?.Property?.PropertyYearBuilt, 0);
                lead.PropertyZip = NullSafe.NullSafeString(dataLead?.Property?.PropertyZip).Replace("Unknown", "");
            }

            return lead;
        }

        
    }
}