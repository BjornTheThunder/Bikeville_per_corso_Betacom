using Microsoft.Data.SqlClient;
using System.Data;

namespace LogErrors
{
    public class ErrorsLog
    {
        private static int GetErrorLine(Exception ex)
        {
            if (!string.IsNullOrEmpty(ex.StackTrace)) 
            { 
                var lines = ex.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None); 
                foreach (var line in lines) 
                { 
                    if (line.Contains("line ")) 
                    { 
                        var parts = line.Split(new[] { "line " }, StringSplitOptions.None); 
                        if (parts.Length > 1 && int.TryParse(parts[1], out int lineNumber)) { return lineNumber; }
                    } 
                } 
            }
            return 0; // Linea non trovata
        }

        public void LogError(Exception ex, SqlConnection conn)
        {
            conn.Open(); 
            using (SqlCommand cmd = new SqlCommand("dbo.uspLogBackendError", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure; 
                cmd.Parameters.AddWithValue("@UserName", " "); // Nome utente corrente
                cmd.Parameters.AddWithValue("@ErrorNumber", ex.HResult); // Numero di errore
                cmd.Parameters.AddWithValue("@ErrorSeverity", 15); // Gravità dell'errore (valore fittizio)
                cmd.Parameters.AddWithValue("@ErrorState", 0); // Stato dell'errore (valore fittizio)
                cmd.Parameters.AddWithValue("@ErrorProcedure", ex.TargetSite?.Name ?? ""); // Nome del metodo
                cmd.Parameters.AddWithValue("@ErrorLine", GetErrorLine(ex)); // Numero della linea, da estrarre dallo stack trace
                cmd.Parameters.AddWithValue("@ErrorMessage", ex.Message); // Messaggio di errore

                SqlParameter outputIdParam = new SqlParameter("@ErrorLogID", SqlDbType.Int) 
                { 
                    Direction = ParameterDirection.Output 
                }; 
                cmd.Parameters.Add(outputIdParam); 
                cmd.ExecuteNonQuery(); 
                int errorLogId = (int)outputIdParam.Value; // Puoi usare errorLogId se necessario
            }
            conn.Close();
        }
        
    }
}
