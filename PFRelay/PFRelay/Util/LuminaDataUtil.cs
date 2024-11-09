using System;
using System.Linq;
using Lumina.Excel.GeneratedSheets;
using PFRelay.Util; // Import LoggerHelper

namespace PFRelay.Util
{
    public static class LuminaDataUtil
    {
        public static string GetJobAbbreviation(uint jobId)
        {
            try
            {
                var jobEnum = Service.DataManager.GetExcelSheet<ClassJob>()?.Where(a => a.RowId == jobId);

                if (jobEnum == null)
                {
                    LoggerHelper.LogError("Failed to retrieve ClassJob data sheet from DataManager.", new Exception("Data sheet retrieval returned null"));
                    return "???";
                }

                var job = jobEnum.DefaultIfEmpty(null).FirstOrDefault();

                if (job == null)
                {
                    LoggerHelper.LogDebug($"Job ID {jobId} not found in ClassJob data sheet.");
                    return "???";
                }

                return job.Abbreviation.ToString();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Error retrieving job abbreviation for Job ID {jobId}", ex);
                return "???";
            }
        }
    }
}
