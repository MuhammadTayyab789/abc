using AlfaService.RequestResponse;
using DataServices.App_Code;
using DataServices.Common;
using DataServices.DataLayer;
using DataServices.DataLayer.DigitalGentrRequestResponse;
using DataServices.DataLayer.StoredProcedureRequestResponse;
using DataServices.DataLayer.OBSCRequestResponse.RapidSafeWatchRequestResponse;
using DataServices.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DataServices.BusinessLogic.Rapid
{
    public class GetRapidAccountOpeningProcessing
    {
        private LoggerAPI _logger = new();
        private List<string> logstr = new List<string>();
        private RequestLogger reqlog = new();
        public ExchangeResponse<cRequestGetRapidCustomer> returnresponse = null;
        private DigitalGentrServices digitalGentrServices = new();
        private OBSCService obsc = new OBSCService();

        public GetRapidAccountOpeningProcessing(RequestData request)
        {
            returnresponse = new ExchangeResponse<cRequestGetRapidCustomer>();
            reqlog.FunctionName = "GetRapidAccountOpeningProcessing";
            cDecription<cRequestGetRapidCustomer> exchangeSession = new();

            try
            {
                returnresponse = exchangeSession.ExchangeSession(request.requestGetRapidCustomer, request.requestGetRapidCustomer.SessionID, request.IMEI, "", "", "", request.Hash);
                request.requestGetRapidCustomer = returnresponse.Data;
            }
            catch (Exception ex)
            {
                reqlog.LogType = AlfaService.Configuration.cEnum.LogType.Error;
                returnresponse.ResponseCode = "01";
                returnresponse.ResponseDesc = AppResponseCodes.AppErrorCodes[returnresponse.ResponseCode].ToString();

                logstr.Add("Exception while Exchanging Session with GetRapidAccountOpeningProcessing " + ex);
            }
        }

        public cResponseData GetCustomerDetails(RequestData requestData)
        {
            reqlog.FunctionName = "GetCustomerDetails";

            cResponseData response = new()
            {
                responseGetRapidAccountProcessing = new cResponseGetRapidAccountProcessing(),
            };

            cEncription<cResponseGetRapidAccountProcessing> encryption = new();
            try
            {
                if (returnresponse.ResponseCode.Equals("00"))
                {
                    reqlog.FunctionName = " GetCustomerDetails | CNIC : " + requestData.requestGetRapidCustomer.CNIC;
                    List<RapidAccountOpeningDetailsWithDocuments> rapidCustomers = GetCustomerData(requestData.requestGetRapidCustomer, requestData.IMEI);

                  //  bool isRegistered = RapidHelper.IsExistingAlfaUser(digitalGentrServices, requestData.requestGetRapidCustomer.CNIC, requestData.IMEI, ref logstr);


                    InsertRapidEventData(requestData.requestGetRapidCustomer, requestData.IMEI);

                    List<CustomerDiscrepencyResponse> customerDiscrepencyResponse = RapidHelper.GetDiscrepentCustomer(digitalGentrServices, requestData.requestGetRapidCustomer.CNIC, ref logstr);
                    List<CorporateCustomerResponse> corporateCustomerResponse = RapidHelper.GetCorporateCustomer(digitalGentrServices, requestData.requestGetRapidCustomer.CNIC, ref logstr);
                    bool isPreviousApplication = RapidHelper.IsExistinAlfaApplication(digitalGentrServices, requestData.requestGetRapidCustomer.CNIC, ref logstr);

                    logstr.Add("isPreviousApplication " + isPreviousApplication);
                    logstr.Add("rapidcust : " + JsonConvert.SerializeObject(rapidCustomers));
                    logstr.Add("customerDiscrepencyResponse: " + JsonConvert.SerializeObject(customerDiscrepencyResponse));


                    if ( rapidCustomers[0].DeviceImei != requestData.IMEI  && (customerDiscrepencyResponse != null || customerDiscrepencyResponse.Count > 0))
                    {
                        response.ResponseCode = "541";
                        response.ResponseDesc = "It Seems You have initiated Account Opening Request From Other Device";
                    }

                    if ( rapidCustomers[0].DeviceImei != requestData.IMEI && rapidCustomers[0].IsProcessed && (customerDiscrepencyResponse == null || customerDiscrepencyResponse.Count <= 0 || !isPreviousApplication))
                    {
                        response.ResponseCode = "541";
                        response.ResponseDesc = AppResponseCodes.AppErrorCodes[response.ResponseCode].ToString();
                    }

                    if (rapidCustomers != null && rapidCustomers.Count > 0 && rapidCustomers[0].DeviceImei ==requestData.IMEI && rapidCustomers[0].IsProcessed && (customerDiscrepencyResponse == null || customerDiscrepencyResponse.Count <= 0 || !isPreviousApplication))
                    {
                        response.ResponseCode = "541";
                        response.ResponseDesc = AppResponseCodes.AppErrorCodes[response.ResponseCode].ToString();
                    }
                    else
                    {
                        response.ResponseCode = "00";
                        response.ResponseDesc = "Not Data Found";
                        if (rapidCustomers != null && rapidCustomers.Count > 0)
                        {
                            response.responseGetRapidAccountProcessing = PrepareRapidCustomerResponse(rapidCustomers);
                            response.responseGetRapidAccountProcessing.IsDiscrepent = Convert.ToString(false);
                            if (customerDiscrepencyResponse != null && customerDiscrepencyResponse.Count > 0)
                            {
                                response.responseGetRapidAccountProcessing.CurrentStep = "1";
                                response.responseGetRapidAccountProcessing.CustomerId = customerDiscrepencyResponse[0].CustomerId;
                                response.responseGetRapidAccountProcessing.IsDiscrepent = Convert.ToString(true);
                            }
                            response.ResponseDesc = AppResponseCodes.AppErrorCodes[response.ResponseCode].ToString();
                        }

                        response.responseGetRapidAccountProcessing.IsCorporateCustomer = Convert.ToString(corporateCustomerResponse != null && corporateCustomerResponse.Count > 0);
                        response.responseGetRapidAccountProcessing.EmployerName = corporateCustomerResponse != null && corporateCustomerResponse.Count > 0 ? corporateCustomerResponse[0].CompanyName : string.Empty;
                        logstr.Add("CPA EmployerName  : " + response.responseGetRapidAccountProcessing.EmployerName);

                    }
                }
                else
                {
                    logstr.Add("Return response  : " + returnresponse.ResponseCode);
                    response.ResponseCode = returnresponse.ResponseCode;
                    response.ResponseDesc = AppResponseCodes.AppErrorCodes[response.ResponseCode].ToString();

                }
            }
            catch (Exception ex)
            {
                reqlog.LogType = AlfaService.Configuration.cEnum.LogType.Error;
                logstr.Add(" GetCustomerDetails Exception : " + ex);
                response.ResponseCode = "534";
                response.ResponseDesc = AppResponseCodes.AppErrorCodes[response.ResponseCode].ToString();

            }
            finally
            {
                SQMService sQMService = new();
                #region :: Encrypting Details
                response.responseGetRapidAccountProcessing.SessionID = returnresponse.SessionID;
                ServiceFunctions _ServiceFunctions = new();
                response.responseGetRapidAccountProcessing.AppURL = _ServiceFunctions.GetSQMUrl(sQMService, requestData.IMEI, returnresponse.LoginName, reqlog.FunctionName, returnresponse.SessionID, returnresponse.OldSessionID, ref logstr);
                response.responseGetRapidAccountProcessing.AppVersion = returnresponse.AppVersion;
                response.responseGetRapidAccountProcessing = encryption.FuncEncripto(response.responseGetRapidAccountProcessing, returnresponse.OldSessionID);
                #endregion
                reqlog.logger = logstr;
                _logger.infos(reqlog);
            }
            return response;
        }

        private void InsertRapidEventData(cRequestGetRapidCustomer requestGetRapidCustomer, string imei)
        {
            try
            {
                RapidEventData rapidEventData = new();
                rapidEventData.CNIC = requestGetRapidCustomer.CNIC;
                rapidEventData.DeviceImei = imei;

                ExecuteProcedureRequest<object> executeProcedureRequest = new()
                {
                    DatabaseName = CML.DataLayer.CEnum.ConnectTo.Ibanking,
                    ProcedureName = "SP_InsertRapidEventData",
                    ProcedureData = rapidEventData
                };
                digitalGentrServices.PutData(executeProcedureRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                reqlog.LogType = AlfaService.Configuration.cEnum.LogType.Error;
                logstr.Add(" InsertRapidEventData Exception : " + ex);
            }
        }

        private List<RapidAccountOpeningDetailsWithDocuments> GetCustomerData(cRequestGetRapidCustomer requestGetRapidCustomer, string imei)
        {
            try
            {
                GetRapidAccountOpeningDataByCNIC rapidDataRequest = new()
                {
                    CNIC = requestGetRapidCustomer.CNIC,
                    DeviceImei = imei
                };
                ExecuteProcedureRequest<object> executeProcedureRequest = new()
                {
                    DatabaseName = CML.DataLayer.CEnum.ConnectTo.Ibanking,
                    ProcedureName = "SP_GetRapidCustomerDetails",
                    ProcedureData = rapidDataRequest
                };
                return digitalGentrServices.GetData<RapidAccountOpeningDetailsWithDocuments>(executeProcedureRequest);
            }
            catch (Exception ex)
            {
                reqlog.LogType = AlfaService.Configuration.cEnum.LogType.Error;
                logstr.Add(" GetCustomerData Exception : " + ex);
            }
            return null;
        }
        private cResponseGetRapidAccountProcessing PrepareRapidCustomerResponse(List<RapidAccountOpeningDetailsWithDocuments> rapidAccountOpening)
        {
            cResponseGetRapidAccountProcessing response = new()
            {
                IdType = rapidAccountOpening[0].IdType,
                CNIC = rapidAccountOpening[0].CNIC,
                Email = rapidAccountOpening[0].Email,
                Mobile = rapidAccountOpening[0].Mobile,
                PersonalDetails = rapidAccountOpening[0].PersonalDetails,
                OccupationDetails = rapidAccountOpening[0].OccupationDetails,
                ContactDetails = rapidAccountOpening[0].ContactDetails,
                BankingDetails = rapidAccountOpening[0].BankingDetails,
                KycDetails = rapidAccountOpening[0].KycDetails,
                FatcaDetails = rapidAccountOpening[0].FatcaDetails,
                CurrentStep = Convert.ToString(rapidAccountOpening[0].CurrentStep),
                IsSignatureDeclaration = Convert.ToString(rapidAccountOpening[0].IsSignatureDeclaration),
                IsBioVerified = Convert.ToString(rapidAccountOpening[0].IsBioVerified),
                IsAsaanAccount = Convert.ToString(rapidAccountOpening[0].IsAsaanAccount),
                Steps = rapidAccountOpening[0].Steps,
                FaceId = string.IsNullOrEmpty(rapidAccountOpening[0].FaceId) ? "" : Convert.ToBase64String(Utility.GetFileAsBytes(rapidAccountOpening[0].FaceId.Replace("https", "http"))),

        };
            List<RapidDocument> documents = new();

            foreach (RapidAccountOpeningDetailsWithDocuments rapidCustomer in rapidAccountOpening)
            {
                if (!string.IsNullOrEmpty(rapidCustomer.Document) && !string.IsNullOrEmpty(rapidCustomer.DocumentName))
                {
                    RapidDocument document = new()
                    {
                        DocumentDetail = rapidCustomer.Document,
                        DocumentName = rapidCustomer.DocumentName,
                        DocumentTypeId = rapidCustomer.DocumentTypeId
                    };
                    documents.Add(document);
                }
            }
            response.Documents = JsonConvert.SerializeObject(documents);
            return response;
        }
    }
}
 CREATE PROCEDURE [dbo].[SP_GetRapidCustomerDetails]    
(@CNIC VARCHAR(17),     
@DeviceImei VARCHAR(50))    
AS     
 BEGIN    
 SELECT ra.CNIC, ra.DeviceImei  ,ra.Mobile,RTRIM(LTRIM(ISNULL(ra.Email, ''))) AS Email, CONVERT(VARCHAR, ra.IssueDate,105) AS IssueDate, ra.PersonalDetails, ra.OccupationDetails, ra.ContactDetails, ra.BankingDetails,     
 ra.KycDetails, ra.FatcaDetails, ra.CurrentStep, ra.IsProcessed, ra.IdType, ra.SignatureDeclaration  As IsSignatureDeclaration, ra.IsBioVerified  AS IsBioVerified , ra.IsAsaanAccount  AS IsAsaanAccount,    
 rad.DocumentId As DocumentTypeId, rad.Document, rdl.DocumentName,ra.Steps ,ra.faceId 
 FROM [dbo].[RapidAccountOpening] ra     
 LEFT OUTER JOIN  RapidAccountOpeningDocuments rad ON ra.ID = rad.ApplicantId /*AND rad.DocumentId NOT IN (18,19)*/  
 LEFT OUTER JOIN RapidDocumentsLookup rdl ON rdl.ID =  rad.DocumentId     
 WHERE CNIC = @CNIC     
--AND DeviceImei = @DeviceImei    
 ;    
 END using AlfaService.RequestResponse;
using DataServices.App_Code;
using DataServices.Common;
using DataServices.DataLayer;
using DataServices.DataLayer.DigitalGentrRequestResponse;
using DataServices.DataLayer.StoredProcedureRequestResponse;
using DataServices.DataLayer.OBSCRequestResponse.RapidSafeWatchRequestResponse;
using DataServices.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DataServices.BusinessLogic.Rapid
{
    public class GetRapidAccountOpeningProcessing
    {
        private LoggerAPI _logger = new();
        private List<string> logstr = new List<string>();
        private RequestLogger reqlog = new();
        public ExchangeResponse<cRequestGetRapidCustomer> returnresponse = null;
        private DigitalGentrServices digitalGentrServices = new();
        private OBSCService obsc = new OBSCService();

        public GetRapidAccountOpeningProcessing(RequestData request)
        {
            returnresponse = new ExchangeResponse<cRequestGetRapidCustomer>();
            reqlog.FunctionName = "GetRapidAccountOpeningProcessing";
            cDecription<cRequestGetRapidCustomer> exchangeSession = new();

            try
            {
                returnresponse = exchangeSession.ExchangeSession(request.requestGetRapidCustomer, request.requestGetRapidCustomer.SessionID, request.IMEI, "", "", "", request.Hash);
                request.requestGetRapidCustomer = returnresponse.Data;
            }
            catch (Exception ex)
            {
                reqlog.LogType = AlfaService.Configuration.cEnum.LogType.Error;
                returnresponse.ResponseCode = "01";
                returnresponse.ResponseDesc = AppResponseCodes.AppErrorCodes[returnresponse.ResponseCode].ToString();

                logstr.Add("Exception while Exchanging Session with GetRapidAccountOpeningProcessing " + ex);
            }
        }

        public cResponseData GetCustomerDetails(RequestData requestData)
        {
            reqlog.FunctionName = "GetCustomerDetails";

            cResponseData response = new()
            {
                responseGetRapidAccountProcessing = new cResponseGetRapidAccountProcessing(),
            };

            cEncription<cResponseGetRapidAccountProcessing> encryption = new();
            try
            {
                if (returnresponse.ResponseCode.Equals("00"))
                {
                    reqlog.FunctionName = " GetCustomerDetails | CNIC : " + requestData.requestGetRapidCustomer.CNIC;
                    List<RapidAccountOpeningDetailsWithDocuments> rapidCustomers = GetCustomerData(requestData.requestGetRapidCustomer, requestData.IMEI);

                  //  bool isRegistered = RapidHelper.IsExistingAlfaUser(digitalGentrServices, requestData.requestGetRapidCustomer.CNIC, requestData.IMEI, ref logstr);

                    InsertRapidEventData(requestData.requestGetRapidCustomer, requestData.IMEI);

                    List<CustomerDiscrepencyResponse> customerDiscrepencyResponse = RapidHelper.GetDiscrepentCustomer(digitalGentrServices, requestData.requestGetRapidCustomer.CNIC, ref logstr);
                    List<CorporateCustomerResponse> corporateCustomerResponse = RapidHelper.GetCorporateCustomer(digitalGentrServices, requestData.requestGetRapidCustomer.CNIC, ref logstr);
                    bool isPreviousApplication = RapidHelper.IsExistinAlfaApplication(digitalGentrServices, requestData.requestGetRapidCustomer.CNIC, ref logstr);

                    logstr.Add("isPreviousApplication " + isPreviousApplication);
                    logstr.Add("rapidcust : " + JsonConvert.SerializeObject(rapidCustomers));
                    logstr.Add("customerDiscrepencyResponse: " + JsonConvert.SerializeObject(customerDiscrepencyResponse));


                    if ( rapidCustomers[0].DeviceImei != requestData.IMEI  && (customerDiscrepencyResponse != null || customerDiscrepencyResponse.Count > 0))
                    {
                        response.ResponseCode = "541";
                        response.ResponseDesc = "It Seems You have initiated Account Opening Request From Other Device";
                        return response,
                    }

                    if ( rapidCustomers[0].DeviceImei != requestData.IMEI  && (customerDiscrepencyResponse == null || customerDiscrepencyResponse.Count <= 0 || !isPreviousApplication))
                    {
                        response.ResponseCode = "541";
                        response.ResponseDesc = AppResponseCodes.AppErrorCodes[response.ResponseCode].ToString();
                        return response
                    }

                    if (rapidCustomers != null && rapidCustomers.Count > 0 && rapidCustomers[0].DeviceImei ==requestData.IMEI && rapidCustomers[0].IsProcessed && (customerDiscrepencyResponse == null || customerDiscrepencyResponse.Count <= 0 || !isPreviousApplication))
                    {
                        response.ResponseCode = "541";
                        response.ResponseDesc = AppResponseCodes.AppErrorCodes[response.ResponseCode].ToString();
                    }
                    else
                    {
                        response.ResponseCode = "00";
                        response.ResponseDesc = "Not Data Found";
                        if (rapidCustomers != null && rapidCustomers.Count > 0 && rapidCustomers[0].DeviceImei == requestData.IMEI)
                        {
                            response.responseGetRapidAccountProcessing = PrepareRapidCustomerResponse(rapidCustomers);
                            response.responseGetRapidAccountProcessing.IsDiscrepent = Convert.ToString(false);
                            if (customerDiscrepencyResponse != null && customerDiscrepencyResponse.Count > 0)
                            {
                                response.responseGetRapidAccountProcessing.CurrentStep = "1";
                                response.responseGetRapidAccountProcessing.CustomerId = customerDiscrepencyResponse[0].CustomerId;
                                response.responseGetRapidAccountProcessing.IsDiscrepent = Convert.ToString(true);
                            }
                            response.ResponseDesc = AppResponseCodes.AppErrorCodes[response.ResponseCode].ToString();
                        }

                        response.responseGetRapidAccountProcessing.IsCorporateCustomer = Convert.ToString(corporateCustomerResponse != null && corporateCustomerResponse.Count > 0);
                        response.responseGetRapidAccountProcessing.EmployerName = corporateCustomerResponse != null && corporateCustomerResponse.Count > 0 ? corporateCustomerResponse[0].CompanyName : string.Empty;
                        logstr.Add("CPA EmployerName  : " + response.responseGetRapidAccountProcessing.EmployerName);

                    }
                }
                else
                {
                    logstr.Add("Return response  : " + returnresponse.ResponseCode);
                    response.ResponseCode = returnresponse.ResponseCode;
                    response.ResponseDesc = AppResponseCodes.AppErrorCodes[response.ResponseCode].ToString();

                }
            }
            catch (Exception ex)
            {
                reqlog.LogType = AlfaService.Configuration.cEnum.LogType.Error;
                logstr.Add(" GetCustomerDetails Exception : " + ex);
                response.ResponseCode = "534";
                response.ResponseDesc = AppResponseCodes.AppErrorCodes[response.ResponseCode].ToString();

            }
            finally
            {
                SQMService sQMService = new();
                #region :: Encrypting Details
                response.responseGetRapidAccountProcessing.SessionID = returnresponse.SessionID;
                ServiceFunctions _ServiceFunctions = new();
                response.responseGetRapidAccountProcessing.AppURL = _ServiceFunctions.GetSQMUrl(sQMService, requestData.IMEI, returnresponse.LoginName, reqlog.FunctionName, returnresponse.SessionID, returnresponse.OldSessionID, ref logstr);
                response.responseGetRapidAccountProcessing.AppVersion = returnresponse.AppVersion;
                response.responseGetRapidAccountProcessing = encryption.FuncEncripto(response.responseGetRapidAccountProcessing, returnresponse.OldSessionID);
                #endregion
                reqlog.logger = logstr;
                _logger.infos(reqlog);
            }
            return response;
        }

        private void InsertRapidEventData(cRequestGetRapidCustomer requestGetRapidCustomer, string imei)
        {
            try
            {
                RapidEventData rapidEventData = new();
                rapidEventData.CNIC = requestGetRapidCustomer.CNIC;
                rapidEventData.DeviceImei = imei;

                ExecuteProcedureRequest<object> executeProcedureRequest = new()
                {
                    DatabaseName = CML.DataLayer.CEnum.ConnectTo.Ibanking,
                    ProcedureName = "SP_InsertRapidEventData",
                    ProcedureData = rapidEventData
                };
                digitalGentrServices.PutData(executeProcedureRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                reqlog.LogType = AlfaService.Configuration.cEnum.LogType.Error;
                logstr.Add(" InsertRapidEventData Exception : " + ex);
            }
        }

        private List<RapidAccountOpeningDetailsWithDocuments> GetCustomerData(cRequestGetRapidCustomer requestGetRapidCustomer, string imei)
        {
            try
            {
                GetRapidAccountOpeningDataByCNIC rapidDataRequest = new()
                {
                    CNIC = requestGetRapidCustomer.CNIC,
                    DeviceImei = imei
                };
                ExecuteProcedureRequest<object> executeProcedureRequest = new()
                {
                    DatabaseName = CML.DataLayer.CEnum.ConnectTo.Ibanking,
                    ProcedureName = "SP_GetRapidCustomerDetails",
                    ProcedureData = rapidDataRequest
                };
                return digitalGentrServices.GetData<RapidAccountOpeningDetailsWithDocuments>(executeProcedureRequest);
            }
            catch (Exception ex)
            {
                reqlog.LogType = AlfaService.Configuration.cEnum.LogType.Error;
                logstr.Add(" GetCustomerData Exception : " + ex);
            }
            return null;
        }
        private cResponseGetRapidAccountProcessing PrepareRapidCustomerResponse(List<RapidAccountOpeningDetailsWithDocuments> rapidAccountOpening)
        {
            cResponseGetRapidAccountProcessing response = new()
            {
                IdType = rapidAccountOpening[0].IdType,
                CNIC = rapidAccountOpening[0].CNIC,
                Email = rapidAccountOpening[0].Email,
                Mobile = rapidAccountOpening[0].Mobile,
                PersonalDetails = rapidAccountOpening[0].PersonalDetails,
                OccupationDetails = rapidAccountOpening[0].OccupationDetails,
                ContactDetails = rapidAccountOpening[0].ContactDetails,
                BankingDetails = rapidAccountOpening[0].BankingDetails,
                KycDetails = rapidAccountOpening[0].KycDetails,
                FatcaDetails = rapidAccountOpening[0].FatcaDetails,
                CurrentStep = Convert.ToString(rapidAccountOpening[0].CurrentStep),
                IsSignatureDeclaration = Convert.ToString(rapidAccountOpening[0].IsSignatureDeclaration),
                IsBioVerified = Convert.ToString(rapidAccountOpening[0].IsBioVerified),
                IsAsaanAccount = Convert.ToString(rapidAccountOpening[0].IsAsaanAccount),
                Steps = rapidAccountOpening[0].Steps,
                FaceId = string.IsNullOrEmpty(rapidAccountOpening[0].FaceId) ? "" : Convert.ToBase64String(Utility.GetFileAsBytes(rapidAccountOpening[0].FaceId.Replace("https", "http"))),

        };
            List<RapidDocument> documents = new();

            foreach (RapidAccountOpeningDetailsWithDocuments rapidCustomer in rapidAccountOpening)
            {
                if (!string.IsNullOrEmpty(rapidCustomer.Document) && !string.IsNullOrEmpty(rapidCustomer.DocumentName))
                {
                    RapidDocument document = new()
                    {
                        DocumentDetail = rapidCustomer.Document,
                        DocumentName = rapidCustomer.DocumentName,
                        DocumentTypeId = rapidCustomer.DocumentTypeId
                    };
                    documents.Add(document);
                }
            }
            response.Documents = JsonConvert.SerializeObject(documents);
            return response;
        }
    }
}
 CREATE PROCEDURE [dbo].[SP_GetRapidCustomerDetails]    
(@CNIC VARCHAR(17),     
@DeviceImei VARCHAR(50))    
AS     
 BEGIN    
 SELECT ra.CNIC, ra.DeviceImei  ,ra.Mobile,RTRIM(LTRIM(ISNULL(ra.Email, ''))) AS Email, CONVERT(VARCHAR, ra.IssueDate,105) AS IssueDate, ra.PersonalDetails, ra.OccupationDetails, ra.ContactDetails, ra.BankingDetails,     
 ra.KycDetails, ra.FatcaDetails, ra.CurrentStep, ra.IsProcessed, ra.IdType, ra.SignatureDeclaration  As IsSignatureDeclaration, ra.IsBioVerified  AS IsBioVerified , ra.IsAsaanAccount  AS IsAsaanAccount,    
 rad.DocumentId As DocumentTypeId, rad.Document, rdl.DocumentName,ra.Steps ,ra.faceId 
 FROM [dbo].[RapidAccountOpening] ra     
 LEFT OUTER JOIN  RapidAccountOpeningDocuments rad ON ra.ID = rad.ApplicantId /*AND rad.DocumentId NOT IN (18,19)*/  
 LEFT OUTER JOIN RapidDocumentsLookup rdl ON rdl.ID =  rad.DocumentId     
 WHERE CNIC = @CNIC     
--AND DeviceImei = @DeviceImei    
 ;    
 END 