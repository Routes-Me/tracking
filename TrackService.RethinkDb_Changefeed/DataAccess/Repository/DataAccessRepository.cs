using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Obfuscation;
using RestSharp;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TrackService.RethinkDb_Abstractions;
using TrackService.RethinkDb_Changefeed.DataAccess.Abstraction;
using TrackService.RethinkDb_Changefeed.Model.Common;

namespace TrackService.RethinkDb_Changefeed.DataAccess.Repository
{
    public class DataAccessRepository : IDataAccessRepository
    {
        private const string DATABASE_NAME = "trackingdb";
        private const string MOBILE_TABLE_NAME = "mobiles";
        private const string CORDINATE_TABLE_NAME = "coordinates";
        public static bool IsAnotherServiceWorking = false;
        private readonly RethinkDb.Driver.RethinkDB _rethinkDbSingleton;
        private readonly Connection _rethinkDbConnection;
        private readonly AppSettings _appSettings;
        private readonly Dependencies _dependencies;

        public DataAccessRepository(IRethinkDbSingletonProvider rethinkDbSingletonProvider, IOptions<AppSettings> appSettings, IOptions<Dependencies> dependencies)
        {
            if (rethinkDbSingletonProvider == null)
            {
                throw new ArgumentNullException(nameof(rethinkDbSingletonProvider));
            }

            _rethinkDbSingleton = rethinkDbSingletonProvider.RethinkDbSingleton;
            _rethinkDbConnection = rethinkDbSingletonProvider.RethinkDbConnection;
            _appSettings = appSettings.Value;
            _dependencies = dependencies.Value;
        }

        public void ChangeVehicleStatus(string vehicleId)
        {
            ReqlFunction1 filter = expr => expr["vehicleId"].Eq(Convert.ToInt32(vehicleId));
            string filterSerialized = ReqlRaw.ToRawString(filter);
            var filterExpr = ReqlRaw.FromRawString(filterSerialized);
            Cursor<object> vehicles = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(filterExpr).Run(_rethinkDbConnection);

            foreach (var vehicle in vehicles)
            {
                var id = JObject.Parse(vehicle.ToString()).Children().Values().LastOrDefault().ToString();

                _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME)
                        .Filter(new { id = id })
                        .Update(new { isLive = false }).Run(_rethinkDbConnection);
            }
        }

        public bool CheckInstitutionExists(string institutionId)
        {
            ReqlFunction1 filter = expr => expr["institutionId"].Eq(Convert.ToInt32(institutionId));
            string filterSerialized = ReqlRaw.ToRawString(filter);
            var filterExpr = ReqlRaw.FromRawString(filterSerialized);
            Cursor<object> institutions = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(filterExpr).Run(_rethinkDbConnection);
            if (institutions.Count() > 0)
                return true;
            else
                return false;
        }

        public bool CheckVehicleByInstitutionExists(string vehicleId, string institutionId)
        {
            ReqlFunction1 filter1 = expr => expr["vehicleId"].Eq(Convert.ToInt32(vehicleId));
            ReqlFunction1 filter2 = expr => expr["institutionId"].Eq(Convert.ToInt32(institutionId));
            string filterSerializedByVehicles = ReqlRaw.ToRawString(filter1);
            string filterSerializedByInstitution = ReqlRaw.ToRawString(filter2);
            var filterExprByVehicles = ReqlRaw.FromRawString(filterSerializedByVehicles);
            var filterExprByInstitution = ReqlRaw.FromRawString(filterSerializedByInstitution);
            Cursor<object> vehicles = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(filterExprByVehicles).Filter(filterExprByInstitution).Run(_rethinkDbConnection);
            if (vehicles.Count() > 0)
                return true;
            else
                return false;
        }

        public bool CheckVehicleExists(string vehicleId)
        {
            ReqlFunction1 filter = expr => expr["vehicleId"].Eq(Convert.ToInt32(vehicleId));
            string filterSerialized = ReqlRaw.ToRawString(filter);
            var filterExpr = ReqlRaw.FromRawString(filterSerialized);
            Cursor<object> vehicles = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(filterExpr).Run(_rethinkDbConnection);
            if (vehicles.Count() > 0)
                return true;
            else
                return false;
        }

        public Task EnsureDatabaseCreated()
        {
            if (!((string[])_rethinkDbSingleton.DbList().Run(_rethinkDbConnection).ToObject<string[]>()).Contains(DATABASE_NAME))
            {
                _rethinkDbSingleton.DbCreate(DATABASE_NAME).Run(_rethinkDbConnection);
            }

            var database = _rethinkDbSingleton.Db(DATABASE_NAME);
            if (!((string[])database.TableList().Run(_rethinkDbConnection).ToObject<string[]>()).Contains(MOBILE_TABLE_NAME))
            {
                database.TableCreate(MOBILE_TABLE_NAME).Run(_rethinkDbConnection);
            }

            if (!((string[])database.TableList().Run(_rethinkDbConnection).ToObject<string[]>()).Contains(CORDINATE_TABLE_NAME))
            {
                database.TableCreate(CORDINATE_TABLE_NAME).Run(_rethinkDbConnection);
            }

            return Task.CompletedTask;
        }

        public async Task<IChangefeed<Coordinates>> GetCoordinatesChangeFeedback(CancellationToken cancellationToken)
        {
            return new RethinkDbChangefeed<Coordinates>(
                await _rethinkDbSingleton.Db(DATABASE_NAME).Table(CORDINATE_TABLE_NAME).Changes().RunChangesAsync<Coordinates>(_rethinkDbConnection, cancellationToken)
            );
        }

        public string GetInstitutionId(string mobileId)
        {
            var vehicle = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Get(mobileId).Run(_rethinkDbConnection);
            string institutionId = string.Empty;

            foreach (var elements in vehicle)
            {
                if (((JProperty)elements).Name == "institutionId")
                {
                    institutionId = ((JProperty)elements).Value.ToString();
                    break;
                }
            }
            return institutionId;
        }

        public string GetVehicleId(string mobileId)
        {
            var vehicle = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Get(mobileId).Run(_rethinkDbConnection);
            string vehicleId = string.Empty;

            foreach (var elements in vehicle)
            {
                if (((JProperty)elements).Name == "vehicleId")
                {
                    vehicleId = ((JProperty)elements).Value.ToString();
                    break;
                }
            }
            return vehicleId;
        }

        public int IdDecryption(string id)
        {
            return ObfuscationClass.DecodeId(Convert.ToInt32(id), _appSettings.PrimeInverse);
        }

        public string IdEncryption(int id)
        {
            return ObfuscationClass.EncodeId(id, _appSettings.Prime).ToString();
        }

        public Task InsertCordinates(CordinatesModel trackingStats)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(Convert.ToDouble(trackingStats.timestamp)).ToLocalTime();
            MobileJSONResponse response = new MobileJSONResponse();
            var vehicleId = Convert.ToInt32(trackingStats.mobileId);
            Cursor<object> vehicle = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(new { vehicleId = vehicleId }).Run(_rethinkDbConnection);
            if (vehicle.BufferedSize == 0)
            {
                var createdVehicle = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Insert(new Mobiles
                    {
                        institutionId = trackingStats.institutionId,
                        vehicleId = vehicleId,
                        isLive = true,
                        timestamp = DateTime.UtcNow
                    }).RunWrite(_rethinkDbConnection);
                response.id = createdVehicle.GeneratedKeys[0].ToString();
            }
            else
            {
                response = JsonConvert.DeserializeObject<MobileJSONResponse>(vehicle.BufferedItems[0].ToString());
                _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME)
                    .Filter(new { id = response.id })
                    .Update(new { timestamp = dtDateTime, isLive = true }).Run(_rethinkDbConnection);
            }

            _rethinkDbSingleton.Db(DATABASE_NAME).Table(CORDINATE_TABLE_NAME).Insert(new Coordinates
            {
                timestamp = dtDateTime,
                latitude = trackingStats.latitude,
                longitude = trackingStats.longitude,
                mobileId = response.id,
                deviceId = trackingStats.deviceId
            }
            ).Run(_rethinkDbConnection);

            return Task.CompletedTask;
        }

        public Task InsertMobiles(MobilesModel mobileModel)
        {
            Cursor<object> vehicle = null;
            Task.Run(() =>
            {
                vehicle = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(new { vehicleId = mobileModel.vehicleId }).Run(_rethinkDbConnection);
            }).Wait();
            if (vehicle.BufferedSize == 0)
            {
                Task.Run(() =>
                {
                    _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Insert(new Mobiles
                    {
                        institutionId = mobileModel.institutionId,
                        vehicleId = mobileModel.vehicleId,
                        isLive = true,
                        timestamp = DateTime.UtcNow
                    }).Run(_rethinkDbConnection);
                }).Wait();
            }
            else
            {
                MobileJSONResponse response = new MobileJSONResponse();
                foreach (var item in vehicle)
                {
                    response = JsonConvert.DeserializeObject<MobileJSONResponse>(item.ToString());
                }
                if (response.isLive == false)
                {
                    _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME)
                   .Filter(new { id = response.id })
                   .Update(new { isLive = true }).Run(_rethinkDbConnection);
                }
            }

            return Task.CompletedTask;
        }

        public bool SuperInstitutions(string tokenInstitutionId)
        {
            bool isSuperInstitutions = false;
            foreach (var item in _appSettings.AllowedSuperInstitutions)
            {
                if (item == tokenInstitutionId)
                    isSuperInstitutions = true;
            }
            return isSuperInstitutions;
        }

        public void SyncCoordinatesToArchiveTable()
        {
            ReqlFunction1 filter = expr => expr["timestamp"].Ge(DateTime.UtcNow.AddHours(-24));
            string filterSerialized = ReqlRaw.ToRawString(filter);
            var filterExpr = ReqlRaw.FromRawString(filterSerialized);
            Cursor<object> coordinates = _rethinkDbSingleton.Db(DATABASE_NAME).Table(CORDINATE_TABLE_NAME).Filter(filterExpr).Run(_rethinkDbConnection);
            List<ArchiveCoordinates> archiveCoordinates = new List<ArchiveCoordinates>();
            decimal latitude = 0, longitude = 0;
            int deviceId = 0, vehicleId = 0;
            string CoordinateId = string.Empty;
            DateTime? timestamp = null;
            foreach (var coordinate in coordinates)
            {
                foreach (var value in JObject.Parse(coordinate.ToString()).Children())
                {
                    if (((JProperty)value).Name.ToString() == "id")
                    {
                        CoordinateId = ((JProperty)value).Value.ToString();
                    }
                    if (((JProperty)value).Name.ToString() == "latitude")
                    {
                        latitude = Convert.ToDecimal(((JProperty)value).Value.ToString());
                    }
                    else if (((JProperty)value).Name.ToString() == "longitude")
                    {
                        longitude = Convert.ToDecimal(((JProperty)value).Value.ToString());
                    }
                    else if (((JProperty)value).Name.ToString() == "timestamp")
                    {
                        var epocTime = ((JProperty)value).Value.ToString();
                        foreach (var timestampVal in JObject.Parse(epocTime).Children())
                        {
                            if (((JProperty)timestampVal).Name.ToString() == "epoch_time")
                            {
                                var UnixTime = ((JProperty)timestampVal).Value.ToString();

                                DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                                timestamp = dateTime.AddSeconds(Convert.ToDouble(UnixTime)).ToLocalTime();
                                break;
                            }
                        }
                    }
                    else if (((JProperty)value).Name.ToString() == "deviceId")
                    {
                        deviceId = Convert.ToInt32(((JProperty)value).Value.ToString());
                    }
                    else if (((JProperty)value).Name.ToString() == "mobileId")
                    {
                        var vehicle = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Get(((JProperty)value).Value.ToString()).Run(_rethinkDbConnection);
                        if (vehicle != null)
                        {
                            foreach (var elements in vehicle)
                            {
                                if (((JProperty)elements).Name == "vehicleId")
                                {
                                    vehicleId = Convert.ToInt32(((JProperty)elements).Value.ToString());
                                    break;
                                }
                            }
                        }
                    }
                }

                archiveCoordinates.Add(new ArchiveCoordinates
                {
                    CoordinateId = CoordinateId,
                    VehicleId = vehicleId,
                    DeviceId = deviceId,
                    Latitude = latitude,
                    Longitude = longitude,
                    Timestamp = timestamp
                });
            }
            if (archiveCoordinates.Count > 0 && archiveCoordinates != null)
            {
                const int pageSize = 120000;
                int pages = archiveCoordinates.Count / pageSize;
                List<ArchiveCoordinates> currentPage;

                for (int i = 0 ; i <= pages ; i++)
                {
                    var client = new RestClient(_appSettings.Host + _dependencies.ArchiveTrackServiceUrl);
                    var request = new RestRequest(Method.POST);
                    currentPage = archiveCoordinates.Skip(i*pageSize).Take(pageSize).ToList();
                    string jsonToSend = JsonConvert.SerializeObject(currentPage);
                    request.AddParameter("application/json; charset=utf-8", jsonToSend, ParameterType.RequestBody);
                    request.RequestFormat = DataFormat.Json;
                    IRestResponse response = client.Execute(request);
                    if (response.StatusCode == HttpStatusCode.Created)
                    {
                        _rethinkDbSingleton.Db(DATABASE_NAME).Table(CORDINATE_TABLE_NAME).Filter(filterExpr).Delete().Run(_rethinkDbConnection);
                    }
                }
            }
        }

        public void SyncVehiclesToArchiveTable()
        {
            ReqlFunction1 filter = expr => expr["timestamp"].Ge(DateTime.UtcNow.AddDays(-7));
            string filterSerialized = ReqlRaw.ToRawString(filter);
            var filterExpr = ReqlRaw.FromRawString(filterSerialized);
            Cursor<object> vehicles = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(filterExpr).Run(_rethinkDbConnection);
            _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(filterExpr).Delete().Run(_rethinkDbConnection);
        }

        public List<IdealVehicleResponse> UpdateVehicleStatus()
        {
            ReqlFunction1 filter = expr => expr["timestamp"].Le(DateTime.UtcNow.AddMinutes(-10));

            string filterSerialized = ReqlRaw.ToRawString(filter);
            var filterExpr = ReqlRaw.FromRawString(filterSerialized);
            Cursor<object> vehicles = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(filterExpr).Filter(new { isLive = true }).Run(_rethinkDbConnection);
            List<IdealVehicleResponse> idealVehicleList = new List<IdealVehicleResponse>();

            if (vehicles.BufferedSize > 0)
            {
                foreach (var vehicle in vehicles)
                {
                    MobileJSONResponse response = JsonConvert.DeserializeObject<MobileJSONResponse>(vehicle.ToString());
                    _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME)
                            .Filter(new { id = response.id })
                            .Update(new { isLive = false }).Run(_rethinkDbConnection);

                    IdealVehicleResponse idealVehicleResponse = new IdealVehicleResponse();
                    idealVehicleResponse.vehicleId = response.vehicleId.ToString();
                    idealVehicleResponse.institutionId = response.institutionId.ToString();
                    idealVehicleList.Add(idealVehicleResponse);
                }
            }
            return idealVehicleList;
        }
    }
}
