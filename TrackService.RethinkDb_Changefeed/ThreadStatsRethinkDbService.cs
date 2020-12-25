using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;
using TrackService.RethinkDb_Abstractions;
using Nancy.Json;
using System.Net;
using TrackService.RethinkDb_Changefeed.Model.Common;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Obfuscation;
using RestSharp;

namespace TrackService.RethinkDb_Changefeed
{
    public class ThreadStatsRethinkDbService : ICoordinateChangeFeedbackBackgroundService
    {
        private const string DATABASE_NAME = "trackingdb";
        private const string MOBILE_TABLE_NAME = "mobiles";
        private const string CORDINATE_TABLE_NAME = "coordinates";

        public static bool IsAnotherServiceWorking = false;

        private readonly RethinkDb.Driver.RethinkDB _rethinkDbSingleton;
        private readonly Connection _rethinkDbConnection;

        private readonly AppSettings _appSettings;
        private readonly Dependencies _dependencies;

        public ThreadStatsRethinkDbService(IRethinkDbSingletonProvider rethinkDbSingletonProvider, IOptions<AppSettings> appSettings, IOptions<Dependencies> dependencies)
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

        public Task InsertCordinates(CordinatesModel trackingStats)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(Convert.ToDouble(trackingStats.timestamp)).ToLocalTime();
            var vehicleId = Convert.ToInt32(trackingStats.mobileId);
            Cursor<object> vehicle = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(new { vehicleId = vehicleId }).Run(_rethinkDbConnection);
            if (vehicle.BufferedSize > 0)
            {
                MobileJSONResponse response = JsonConvert.DeserializeObject<MobileJSONResponse>(vehicle.BufferedItems[0].ToString());

                _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME)
                        .Filter(new { id = response.id })
                        .Update(new { timestamp = dtDateTime, isLive = true }).Run(_rethinkDbConnection);

                _rethinkDbSingleton.Db(DATABASE_NAME).Table(CORDINATE_TABLE_NAME).Insert(new Coordinates
                {
                    timestamp = dtDateTime,
                    latitude = trackingStats.latitude,
                    longitude = trackingStats.longitude,
                    mobileId = response.id,
                    deviceId = trackingStats.deviceId
                }
                ).Run(_rethinkDbConnection);
            }

            return Task.CompletedTask;
        }

        public async Task<IChangefeed<Coordinates>> GetCoordinatesChangeFeedback(CancellationToken cancellationToken)
        {
            return new RethinkDbChangefeed<Coordinates>(
                await _rethinkDbSingleton.Db(DATABASE_NAME).Table(CORDINATE_TABLE_NAME).Changes().RunChangesAsync<Coordinates>(_rethinkDbConnection, cancellationToken)
            );
        }

        public async Task<IChangefeed<Mobiles>> GetMobileChangeFeedback(CancellationToken cancellationToken)
        {
            return new RethinkDbChangefeed<Mobiles>(
                await _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Changes().RunChangesAsync<Mobiles>(_rethinkDbConnection, cancellationToken)
            );
        }

        public async Task<dynamic> GetAllVehicleByInstitutionId(IdleModel model)
        {
            try
            {
                var keys = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Run(_rethinkDbConnection);
                DateTime startDate;
                DateTime endDate;
                string filterSerializedForLive = string.Empty;
                Cursor<object> vehicles;
                VehicleResponse oVehicleResponse = new VehicleResponse();

                ReqlFunction1 filterForinstitutionId = expr => expr["institutionId"].Eq(Convert.ToInt32(model.institutionId));
                string filterSerializedForinstitutionId = ReqlRaw.ToRawString(filterForinstitutionId);
                var filterExprForinstitutionId = ReqlRaw.FromRawString(filterSerializedForinstitutionId);

                Cursor<object> InstitutionData = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(filterExprForinstitutionId).Run(_rethinkDbConnection);
                if (InstitutionData.BufferedSize == 0)
                    return ReturnResponse.ErrorResponse("Institution does not exists in database.", StatusCodes.Status404NotFound);

                ReqlFunction1 filterForLive = expr => expr["isLive"].Eq(false);
                filterSerializedForLive = ReqlRaw.ToRawString(filterForLive);
                var filterExprForLive = ReqlRaw.FromRawString(filterSerializedForLive);

                if (!string.IsNullOrEmpty(Convert.ToString(model.startAt)) && !string.IsNullOrEmpty(Convert.ToString(model.endAt)) && DateTime.TryParse(Convert.ToString(model.startAt), out startDate) && DateTime.TryParse(Convert.ToString(model.endAt), out endDate))
                {
                    ReqlFunction1 filterForStartDate = expr => expr["timestamp"].Ge(startDate);
                    string filterSerializedForStartDate = ReqlRaw.ToRawString(filterForStartDate);
                    var filterExprForStartDate = ReqlRaw.FromRawString(filterSerializedForStartDate);

                    ReqlFunction1 filterForEndDate = expr => expr["timestamp"].Le(endDate);
                    string filterSerializedForEndDate = ReqlRaw.ToRawString(filterForEndDate);
                    var filterExprForEndDate = ReqlRaw.FromRawString(filterSerializedForEndDate);

                    vehicles = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(filterExprForinstitutionId).Filter(filterExprForLive).Filter(filterForStartDate).Filter(filterForEndDate).Run(_rethinkDbConnection);
                }
                else
                {
                    vehicles = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(filterExprForinstitutionId).Filter(filterExprForLive).Run(_rethinkDbConnection);
                }

                List<VehicleDetails> listVehicles = new List<VehicleDetails>();
                List<CoordinatesDetail> listCoordinates = new List<CoordinatesDetail>();

                foreach (var vehicle in vehicles)
                {
                    string VehicleId = string.Empty, institutionId = string.Empty, DeviceId = string.Empty;
                    foreach (var value in JObject.Parse(vehicle.ToString()).Children())
                    {

                        if (((JProperty)value).Name.ToString() == "id")
                        {

                            ReqlFunction1 cordinatefilter = expr => expr["mobileId"].Eq(((JProperty)value).Value.ToString());
                            string cordinatefilterSerialized = ReqlRaw.ToRawString(cordinatefilter);
                            var cordinatefilterExpr = ReqlRaw.FromRawString(cordinatefilterSerialized);
                            Cursor<object> coordinates = _rethinkDbSingleton.Db(DATABASE_NAME).Table(CORDINATE_TABLE_NAME).Filter(cordinatefilterExpr).Run(_rethinkDbConnection);

                            foreach (var coordinate in coordinates)
                            {
                                string latitude = string.Empty, longitude = string.Empty, timestamp = string.Empty;
                                foreach (var cordinatevalue in JObject.Parse(coordinate.ToString()).Children())
                                {

                                    if (((JProperty)cordinatevalue).Name.ToString() == "latitude")
                                    {
                                        latitude = ((JProperty)cordinatevalue).Value.ToString();
                                    }
                                    else if (((JProperty)cordinatevalue).Name.ToString() == "longitude")
                                    {
                                        longitude = ((JProperty)cordinatevalue).Value.ToString();
                                    }
                                    else if (((JProperty)cordinatevalue).Name.ToString() == "timestamp")
                                    {
                                        var epocTime = ((JProperty)cordinatevalue).Value.ToString();
                                        foreach (var timestampVal in JObject.Parse(epocTime).Children())
                                        {
                                            if (((JProperty)timestampVal).Name.ToString() == "epoch_time")
                                            {
                                                var UnixTime = ((JProperty)timestampVal).Value.ToString();

                                                System.DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                                                timestamp = dateTime.AddSeconds(Convert.ToDouble(UnixTime)).ToLocalTime().ToString();
                                                break;
                                            }
                                        }
                                    }
                                    else if (((JProperty)cordinatevalue).Name.ToString() == "deviceId")
                                    {
                                        DeviceId = ((JProperty)cordinatevalue).Value.ToString();
                                    }
                                }
                                listCoordinates.Add(new CoordinatesDetail
                                {
                                    latitude = Convert.ToDouble(latitude),
                                    longitude = Convert.ToDouble(longitude),
                                    timestamp = timestamp
                                });
                            }
                        }
                        else if (((JProperty)value).Name.ToString() == "vehicleId")
                        {
                            VehicleId = ((JProperty)value).Value.ToString();
                        }
                        else if (((JProperty)value).Name.ToString() == "institutionId")
                        {
                            institutionId = ((JProperty)value).Value.ToString();
                        }
                    }
                    listVehicles.Add(new VehicleDetails
                    {
                        deviceId = DeviceId,
                        vehicleId = VehicleId,
                        institutionId = institutionId,
                        coordinates = listCoordinates
                    });
                }

                if (listVehicles == null || listVehicles.Count == 0)
                    return ReturnResponse.ErrorResponse("Vehicle not found.", StatusCodes.Status404NotFound);

                oVehicleResponse.status = true;
                oVehicleResponse.message = "Vehicle retrived successfully.";
                oVehicleResponse.statusCode = StatusCodes.Status200OK;
                oVehicleResponse.data = listVehicles;
                return oVehicleResponse;
            }
            catch (Exception ex)
            {
                return ReturnResponse.ExceptionResponse(ex);
            }
        }

        public async Task<dynamic> GetAllVehicleDetail(Pagination pageInfo, IdleModel model)
        {
            try
            {
                var keys = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Run(_rethinkDbConnection);
                DateTime startDate;
                DateTime endDate;
                Cursor<object> vehicles;
                string filterSerialized = string.Empty;
                VehicleResponse oVehicleResponse = new VehicleResponse();

                ReqlFunction1 filter = expr => expr["isLive"].Eq(false);
                filterSerialized = ReqlRaw.ToRawString(filter);
                var filterExpr = ReqlRaw.FromRawString(filterSerialized);

                if (!string.IsNullOrEmpty(Convert.ToString(model.startAt)) && !string.IsNullOrEmpty(Convert.ToString(model.endAt)) && DateTime.TryParse(Convert.ToString(model.startAt), out startDate) && DateTime.TryParse(Convert.ToString(model.endAt), out endDate))
                {
                    ReqlFunction1 filterForStartDate = expr => expr["timestamp"].Ge(startDate);
                    string filterSerializedForStartDate = ReqlRaw.ToRawString(filterForStartDate);
                    var filterExprForStartDate = ReqlRaw.FromRawString(filterSerializedForStartDate);

                    ReqlFunction1 filterForEndDate = expr => expr["timestamp"].Le(endDate);
                    string filterSerializedForEndDate = ReqlRaw.ToRawString(filterForEndDate);
                    var filterExprForEndDate = ReqlRaw.FromRawString(filterSerializedForEndDate);

                    vehicles = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(filterExpr).Filter(filterForStartDate).Filter(filterForEndDate).Run(_rethinkDbConnection);
                }
                else
                {
                    vehicles = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(filterExpr).Run(_rethinkDbConnection);
                }

                List<VehicleDetails> listVehicles = new List<VehicleDetails>();
                List<CoordinatesDetail> listCoordinates = new List<CoordinatesDetail>();

                int totalCount = 0;
                foreach (var vehicle in vehicles)
                {
                    totalCount++;
                    string VehicleId = string.Empty, institutionId = string.Empty, DeviceId = string.Empty;
                    foreach (var value in JObject.Parse(vehicle.ToString()).Children())
                    {

                        if (((JProperty)value).Name.ToString() == "id")
                        {
                            ReqlFunction1 cordinatefilter = expr => expr["mobileId"].Eq(((JProperty)value).Value.ToString());
                            string cordinatefilterSerialized = ReqlRaw.ToRawString(cordinatefilter);
                            var cordinatefilterExpr = ReqlRaw.FromRawString(cordinatefilterSerialized);
                            Cursor<object> coordinates = _rethinkDbSingleton.Db(DATABASE_NAME).Table(CORDINATE_TABLE_NAME).Filter(cordinatefilterExpr).Run(_rethinkDbConnection);

                            foreach (var coordinate in coordinates)
                            {
                                string latitude = string.Empty, longitude = string.Empty, timestamp = string.Empty;
                                foreach (var cordinatevalue in JObject.Parse(coordinate.ToString()).Children())
                                {

                                    if (((JProperty)cordinatevalue).Name.ToString() == "latitude")
                                    {
                                        latitude = ((JProperty)cordinatevalue).Value.ToString();
                                    }
                                    else if (((JProperty)cordinatevalue).Name.ToString() == "longitude")
                                    {
                                        longitude = ((JProperty)cordinatevalue).Value.ToString();
                                    }
                                    else if (((JProperty)cordinatevalue).Name.ToString() == "timestamp")
                                    {
                                        var epocTime = ((JProperty)cordinatevalue).Value.ToString();
                                        foreach (var timestampVal in JObject.Parse(epocTime).Children())
                                        {
                                            if (((JProperty)timestampVal).Name.ToString() == "epoch_time")
                                            {
                                                var UnixTime = ((JProperty)timestampVal).Value.ToString();

                                                System.DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                                                timestamp = dateTime.AddSeconds(Convert.ToDouble(UnixTime)).ToLocalTime().ToString();
                                                break;
                                            }
                                        }
                                    }
                                    else if (((JProperty)cordinatevalue).Name.ToString() == "deviceId")
                                    {
                                        DeviceId = ((JProperty)cordinatevalue).Value.ToString();
                                    }
                                }
                                listCoordinates.Add(new CoordinatesDetail
                                {
                                    latitude = Convert.ToDouble(latitude),
                                    longitude = Convert.ToDouble(longitude),
                                    timestamp = timestamp
                                });
                            }
                        }
                        else if (((JProperty)value).Name.ToString() == "vehicleId")
                        {
                            VehicleId = ((JProperty)value).Value.ToString();
                        }
                        else if (((JProperty)value).Name.ToString() == "institutionId")
                        {
                            institutionId = ((JProperty)value).Value.ToString();
                        }
                    }

                    listVehicles.Add(new VehicleDetails
                    {
                        deviceId = DeviceId,
                        vehicleId = VehicleId,
                        institutionId = institutionId,
                        coordinates = listCoordinates
                    });
                }

                var page = new Pagination
                {
                    offset = pageInfo.offset,
                    limit = pageInfo.limit,
                    total = totalCount
                };

                if (listVehicles == null || listVehicles.Count == 0)
                    return ReturnResponse.ErrorResponse("Vehicle not found.", StatusCodes.Status404NotFound);

                oVehicleResponse.status = true;
                oVehicleResponse.message = "Vehicle retrived successfully.";
                oVehicleResponse.statusCode = StatusCodes.Status200OK;
                oVehicleResponse.pagination = page;
                oVehicleResponse.data = listVehicles;
                return oVehicleResponse;

            }
            catch (Exception ex)
            {
                return ReturnResponse.ExceptionResponse(ex);
            }
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

        // This is called from background service Monitor Vehicle
        public void UpdateVehicleStatus()
        {
            ReqlFunction1 filter = expr => expr["timestamp"].Le(DateTime.UtcNow.AddMinutes(-2));
            string filterSerialized = ReqlRaw.ToRawString(filter);
            var filterExpr = ReqlRaw.FromRawString(filterSerialized);
            Cursor<object> vehicles = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(filterExpr).Run(_rethinkDbConnection);

            if (vehicles.BufferedSize > 0)
            {
                foreach (var vehicle in vehicles)
                {
                    MobileJSONResponse response = JsonConvert.DeserializeObject<MobileJSONResponse>(vehicle.ToString());
                    _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME)
                            .Filter(new { id = response.id })
                            .Update(new { isLive = false }).Run(_rethinkDbConnection);
                }
            }
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

        public void SyncCoordinatesToArchiveTable()
        {
            try
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

                                    System.DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
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

                    var client = new RestClient(_appSettings.Host + _dependencies.ArchiveTrackServiceUrl);
                    var request = new RestRequest(Method.POST);
                    string jsonToSend = JsonConvert.SerializeObject(archiveCoordinates);
                    request.AddParameter("application/json; charset=utf-8", jsonToSend, ParameterType.RequestBody);
                    request.RequestFormat = DataFormat.Json;
                    IRestResponse response = client.Execute(request);
                    if (response.StatusCode != HttpStatusCode.Created)
                    {
                        _rethinkDbSingleton.Db(DATABASE_NAME).Table(CORDINATE_TABLE_NAME).Filter(filterExpr).Delete().Run(_rethinkDbConnection);
                    }
                }
            }
            catch (Exception ex)
            {
                var m = ex.Message;
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

        public Task InsertMobiles(MobilesModel model)
        {
            Cursor<object> vehicle = null;
            Task.Run(() =>
            {
                vehicle = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(new { vehicleId = model.vehicleId }).Run(_rethinkDbConnection);
            }).Wait();
            if (vehicle.BufferedSize == 0)
            {
                Task.Run(() =>
                {
                    _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Insert(new Mobiles
                    {
                        institutionId = model.institutionId,
                        vehicleId = model.vehicleId,
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

        public int IdDecryption(string id)
        {
            return ObfuscationClass.DecodeId(Convert.ToInt32(id), _appSettings.PrimeInverse);
        }

        public string IdEncryption(int id)
        {
            return ObfuscationClass.EncodeId(id, _appSettings.Prime).ToString();
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

        
    }
}