using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TrackService.Dtos;

namespace TrackService.Services
{
    public class CheckinService : ICheckinService
    {
        private readonly HttpClient _httpClient;
        // private readonly UserInfo _userInfo;

        private readonly string _remoteServiceBaseUrl;

        public CheckinService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _remoteServiceBaseUrl = "";
        }

        // public async Task<Catalog> GetCatalogItems(int page, int take,
        //                                            int? brand, int? type)
        // {
        //     var uri = API.Catalog.GetAllCatalogItems(_remoteServiceBaseUrl,
        //                                              page, take, brand, type);

        //     var responseString = await _httpClient.GetStringAsync(uri);

        //     var catalog = JsonConvert.DeserializeObject<Catalog>(responseString);
        //     return catalog;
        // }

        public async Task<CheckinReadDto> PostCheckin(CheckinCreateDto checkinCreateDto)
        {
            var uri = "http://vmtprojectstage.uaenorth.cloudapp.azure.com:108/v1.0/checkins";
            // var uri = API.Checkin.PostCheckin(_remoteServiceBaseUrl);
            var json = JsonConvert.SerializeObject(checkinCreateDto);
            var stringContent = new StringContent(json, System.Text.UnicodeEncoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);
            // _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "");
            var responseString = await _httpClient.PostAsync(uri, stringContent);
            // return JsonConvert.DeserializeObject<CheckinReadDto>(responseString);
            return new CheckinReadDto();
        }
    }

    enum OperationKind
    {
        Connected,
        Disconnected
    }
}