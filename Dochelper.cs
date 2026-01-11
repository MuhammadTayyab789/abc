using System.Data;
using System.Net;
using System.Data.SqlClient;
using DocumentUploadUtility.RequestandResponse;
using Serilog;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text;
using System.Diagnostics;
namespace DocumentUploadUtility.BusinessLogic
{
    public class Dochelper
    {
        public static List<CustomerDocuments> GetCustomerDocuments(string cnic, string imei)
        {

            List<CustomerDocuments> customerDocuments = new();

            try
            {
                string Ibanking = "";
                using (SqlConnection connection = new(Ibanking))
                {
                    using (SqlCommand command = new("SP_GetRapidCustomerDocuments", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        command.Parameters.Add("@CNIC", SqlDbType.NVarChar).Value = cnic;
                        command.Parameters.Add("@DeviceImei", SqlDbType.NVarChar).Value = imei;

                        if (connection.State == ConnectionState.Closed)
                        {
                            connection.Open();
                        }

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                CustomerDocuments doc = new()
                                {
                                    DocumentName = reader["DocumentName"]?.ToString(),
                                    Document = reader["Document"]?.ToString(),
                                    // Map other fields as per your schema
                                };

                                customerDocuments.Add(doc);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                return null;
            }

            return customerDocuments;
        }










        public static int SaveCustomerImages(string sessionId, List<CustomerDocuments> rapidAccountOpeningDetailsWithDocuments, bool isAsaanAccountWithoutSignature)
        {
            try
            {

                string RapidConnectionStringForImages = string.Empty;

                using (SqlConnection connection = new(RapidConnectionStringForImages))
                {
                    using (SqlCommand command = new("sp_Alfacustomerpostdata", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add("@Sessionid", SqlDbType.Int).Value = Convert.ToString(sessionId);
                        bool isSignatureSet = false;
                        bool isProofOfIncomeSet = false;
                        for (int i = 0; i < rapidAccountOpeningDetailsWithDocuments.Count; i++)
                        {
                            //if (isAsaanAccountWithoutSignature && !isSignatureSet)
                            //{
                            //    isSignatureSet = true;
                            //    command.Parameters.Add("@DigitalSignatureImage", SqlDbType.VarBinary).Value = GetFileAsBytes(ConfigKeys.GetConfigKeysDataTable().Rows[0]["AsaanAccountDefaultSignature"].ToString().Replace("https", "http"));
                            //    command.Parameters.Add("@Signature_File_Name", SqlDbType.NVarChar).Value = sessionId + "_" + rapidAccountOpeningDetailsWithDocuments[i].DocumentName + ".jpeg";
                            //    command.Parameters.Add("@Signature_File_ContentType", SqlDbType.NVarChar).Value = "image/jpeg";
                            //}
                            if (rapidAccountOpeningDetailsWithDocuments[i].DocumentName == "Signature" && !isSignatureSet)
                            {
                                isSignatureSet = true;
                                command.Parameters.Add("@DigitalSignatureImage", SqlDbType.VarBinary).Value = GetFileAsBytes(rapidAccountOpeningDetailsWithDocuments[i].Document.Replace("https", "http"));
                                command.Parameters.Add("@Signature_File_Name", SqlDbType.NVarChar).Value = sessionId + "_" + rapidAccountOpeningDetailsWithDocuments[i].DocumentName + ".jpeg";
                                command.Parameters.Add("@Signature_File_ContentType", SqlDbType.NVarChar).Value = "image/jpeg";
                            }
                            else if (rapidAccountOpeningDetailsWithDocuments[i].DocumentName == "CNIC Front")
                            {
                                command.Parameters.Add("@IDFrontImage", SqlDbType.VarBinary).Value = GetFileAsBytes(rapidAccountOpeningDetailsWithDocuments[i].Document.Replace("https", "http"));
                                command.Parameters.Add("@CNIC_F_File_Name", SqlDbType.NVarChar).Value = sessionId + "_" + rapidAccountOpeningDetailsWithDocuments[i].DocumentName + ".jpeg";
                                command.Parameters.Add("@CNIC_F_File_ContentType", SqlDbType.NVarChar).Value = "image/jpeg";
                            }

                            else if (rapidAccountOpeningDetailsWithDocuments[i].DocumentName == "CNIC Back")
                            {
                                command.Parameters.Add("@IDBackImage", SqlDbType.VarBinary).Value = GetFileAsBytes(rapidAccountOpeningDetailsWithDocuments[i].Document.Replace("https", "http"));
                                command.Parameters.Add("@CNIC_B_File_Name", SqlDbType.NVarChar).Value = sessionId + "_" + rapidAccountOpeningDetailsWithDocuments[i].DocumentName + ".jpeg";
                                command.Parameters.Add("@CNIC_B_File_ContentType", SqlDbType.NVarChar).Value = "image/jpeg";
                            }
                            else if (rapidAccountOpeningDetailsWithDocuments[i].DocumentName == "Proof of Income" && !isProofOfIncomeSet)
                            {
                                command.Parameters.Add("@proofOfIncomeImage", SqlDbType.VarBinary).Value = GetFileAsBytes(rapidAccountOpeningDetailsWithDocuments[i].Document.Replace("https", "http"));
                                command.Parameters.Add("@ProofIncome_File_Name", SqlDbType.NVarChar).Value = sessionId + "_" + rapidAccountOpeningDetailsWithDocuments[i].DocumentName + GetExtension(rapidAccountOpeningDetailsWithDocuments[i].Document.Replace("https", "http"));
                                command.Parameters.Add("@ProofIncome_File_ContentType", SqlDbType.NVarChar).Value = GetContentType(rapidAccountOpeningDetailsWithDocuments[i].Document.Replace("https", "http"));
                            }
                            else if (rapidAccountOpeningDetailsWithDocuments[i].DocumentName == "IncomeDeclaration" && !isProofOfIncomeSet)
                            {
                                command.Parameters.Add("@proofOfIncomeImage", SqlDbType.VarBinary).Value = GetFileAsBytes(rapidAccountOpeningDetailsWithDocuments[i].Document.Replace("https", "http"));
                                command.Parameters.Add("@ProofIncome_File_Name", SqlDbType.NVarChar).Value = sessionId + "_" + rapidAccountOpeningDetailsWithDocuments[i].DocumentName + GetExtension(rapidAccountOpeningDetailsWithDocuments[i].Document.Replace("https", "http"));
                                command.Parameters.Add("@ProofIncome_File_ContentType", SqlDbType.NVarChar).Value = GetContentType(rapidAccountOpeningDetailsWithDocuments[i].Document.Replace("https", "http"));
                            }
                            else if (rapidAccountOpeningDetailsWithDocuments[i].DocumentName == "Proof of Address")
                            {
                                command.Parameters.Add("@ProofOfNRPImage", SqlDbType.VarBinary).Value = GetFileAsBytes(rapidAccountOpeningDetailsWithDocuments[i].Document.Replace("https", "http"));
                                command.Parameters.Add("@NRP_File_Name", SqlDbType.NVarChar).Value = sessionId + "_" + rapidAccountOpeningDetailsWithDocuments[i].DocumentName + GetExtension(rapidAccountOpeningDetailsWithDocuments[i].Document.Replace("https", "http"));
                                command.Parameters.Add("@NRP_File_ContentType", SqlDbType.NVarChar).Value = GetContentType(rapidAccountOpeningDetailsWithDocuments[i].Document.Replace("https", "http"));
                            }
                            else if (rapidAccountOpeningDetailsWithDocuments[i].DocumentName == "Miscellaneous" || rapidAccountOpeningDetailsWithDocuments[i].DocumentName == "ProofofNonResidence")
                            {
                                command.Parameters.Add("@ZakatDeclaration", SqlDbType.VarBinary).Value = GetFileAsBytes(rapidAccountOpeningDetailsWithDocuments[i].Document.Replace("https", "http"));
                                command.Parameters.Add("@ZakatFileName", SqlDbType.NVarChar).Value = sessionId + "_" + rapidAccountOpeningDetailsWithDocuments[i].DocumentName + GetExtension(rapidAccountOpeningDetailsWithDocuments[i].Document.Replace("https", "http"));
                                command.Parameters.Add("@ZakatFileContent", SqlDbType.NVarChar).Value = GetContentType(rapidAccountOpeningDetailsWithDocuments[i].Document.Replace("https", "http"));
                            }


                        }
                        if (connection.State == ConnectionState.Closed)
                        {
                            connection.Open();
                        }
                        return command.ExecuteNonQuery();
                    }

                }
            }
            catch (Exception ex)
            {

                return 0;
            }

        }

        public static byte[] GetFileAsBytes(string uri)
        {
            var webClient = new WebClient();
            webClient.Headers.Add(HttpRequestHeader.UserAgent, "\"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36\"");


            int retries = 3;
            int delayMs = 1000; // 1 second

            for (int i = 0; i < retries; i++)
            {
                try
                {
                    return webClient.DownloadData(uri);
                }
                catch (WebException ex) when (((HttpWebResponse)ex.Response)?.StatusCode == (HttpStatusCode)429)
                {
                    // Wait and retry
                    System.Threading.Thread.Sleep(delayMs);
                    delayMs *= 2; // exponential backoff
                }
            }

            throw new Exception("Failed to download file after multiple retries due to 429");


        }

        public static string GetContentType(string uri)
        {
            if (uri.EndsWith("pdf"))
            {
                return "application/pdf";
            }
            return "image/jpeg";
        }

        public static string GetExtension(string uri)
        {
            if (uri.EndsWith("pdf"))
            {
                return ".pdf";
            }
            return ".jpeg";
        }
        public static string DocumentUploadScriptbuilder(string sessionId, List<CustomerDocuments> documents, bool isAsaanAccountWithoutSignature)
        {
            bool isSignatureSet = false;
            bool isProofOfIncomeSet = false;

            var sb = new StringBuilder();
            sb.AppendLine($"EXEC sp_Alfacustomerpostdata");
            sb.AppendLine($"    @Sessionid = {sessionId}");

            foreach (var doc in documents)
            {
                byte[] docBytes = GetFileAsBytes(doc.Document);
               
              //  string extension = GetExtension(doc.Document);
               
                string ext = GetExtension(doc.Document);
                string filePath = SaveBytesToFile(docBytes, doc.DocumentName, true);
                 Log.Information("filepath"+ filePath);
                string contentType = GetContentType(doc.Document);

                // Signature logic
                if ((isAsaanAccountWithoutSignature && !isSignatureSet) || (doc.DocumentName == "Signature" && !isSignatureSet))
                {
                    if (isAsaanAccountWithoutSignature && !isSignatureSet && doc.DocumentName != "Signature")
                    {
                        docBytes = GetFileAsBytes("https://example.com/default-signature.jpeg");
                    }

                    sb.AppendLine($",    @DigitalSignatureImage = {ToSqlServerBlob(docBytes)}");
                    sb.AppendLine($",    @Signature_File_Name = '{sessionId}_{doc.DocumentName}.jpeg'");
                    sb.AppendLine($",    @Signature_File_ContentType = 'image/jpeg'");
                    isSignatureSet = true;
                    continue;
                }

                // CNIC Front
                if (doc.DocumentName == "CNIC Front")
                {
                    sb.AppendLine($",    @IDFrontImage = {ToSqlServerBlob(docBytes)}");
                    sb.AppendLine($",    @CNIC_F_File_Name = '{sessionId}_{doc.DocumentName}.jpeg'");
                    sb.AppendLine($",    @CNIC_F_File_ContentType = 'image/jpeg'");
                    continue;
                }

                // CNIC Back
                if (doc.DocumentName == "CNIC Back")
                {
                    sb.AppendLine($",    @IDBackImage = {ToSqlServerBlob(docBytes)}");
                    sb.AppendLine($",    @CNIC_B_File_Name = '{sessionId}_{doc.DocumentName}.jpeg'");
                    sb.AppendLine($",    @CNIC_B_File_ContentType = 'image/jpeg'");
                    continue;
                }

                // Proof of Income / IncomeDeclaration
                if ((doc.DocumentName == "Proof of Income" || doc.DocumentName == "IncomeDeclaration") && !isProofOfIncomeSet)
                {
                    sb.AppendLine($",    @proofOfIncomeImage = {ToSqlServerBlob(docBytes)}");
                    sb.AppendLine($",    @ProofIncome_File_Name = '{sessionId}_{doc.DocumentName}{ext}'");
                    sb.AppendLine($",    @ProofIncome_File_ContentType = '{contentType}'");
                    isProofOfIncomeSet = true;
                    continue;
                }

                // Proof of Address
                if (doc.DocumentName == "Proof of Address")
                {
                    sb.AppendLine($",    @ProofOfNRPImage = {ToSqlServerBlob(docBytes)}");
                    sb.AppendLine($",    @NRP_File_Name = '{sessionId}_{doc.DocumentName}{ext}'");
                    sb.AppendLine($",    @NRP_File_ContentType = '{contentType}'");
                    continue;
                }

                // Miscellaneous / ProofofNonResidence
                if (doc.DocumentName == "Miscellaneous" || doc.DocumentName == "ProofofNonResidence")
                {
                    sb.AppendLine($",    @ZakatDeclaration = {ToSqlServerBlob(docBytes)}");
                    sb.AppendLine($",    @ZakatFileName = '{sessionId}_{doc.DocumentName}{ext}'");
                    sb.AppendLine($",    @ZakatFileContent = '{contentType}'");
                    continue;
                }
            }

            return sb.ToString();
        }

        // Convert byte[] to SQL VARBINARY hex string
        private static string ToSqlServerBlob(byte[] data)
        {
            return (data == null || data.Length == 0) ? "NULL" : "0x" + BitConverter.ToString(data).Replace("-", "");
        }


        public static string SaveBytesToFile(byte[] fileBytes, string fileName, bool openFile = false)
        {
            // Choose a temp folder to save files
            string folderPath = Path.Combine(Path.GetTempPath(), "DocumentCheck");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, fileName);

            // Save bytes to file
            File.WriteAllBytes(filePath+"jpg", fileBytes);

            // Optionally open the file
            if (openFile)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }

            return filePath;
        }
    }

}


