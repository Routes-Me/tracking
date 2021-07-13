namespace API
{
    class Checkin
    {
        static private string Root
        {
            get
            {
                return "/v1.0/checkins";
            }
        }

        static private string Path(string baseUrl, string destination) {
            return baseUrl + destination;
        } 
        static public string PostCheckin(string baseUrl)
        {
            return Path(baseUrl, Root);
        }
    }
}