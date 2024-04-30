namespace Business_Layer.Utilities
{
    public static class DateHelper
    {

        public static DateTime? GetDOBDateTime(int? year, string? month, int? date)
        {
            if (year != null && !string.IsNullOrEmpty(month) && date != null)
            {
                string dobDate = year.Value.ToString("D4") + "-" + Convert.ToInt32(month).ToString("D2") + "-" + date.Value.ToString("D2");
                return DateTime.Parse(dobDate);
            }

            return null;
        }
    }
}
