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
            dtDateTime = dtDateTime.AddSeconds(Convert.ToDouble(trackingStats.timeStamp)).ToLocalTime();
            var vehicleId = Convert.ToInt32(trackingStats.mobileId);
            Cursor<object> vehicle = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(new { vehicleId = vehicleId }).Run(_rethinkDbConnection);
            if (vehicle.BufferedSize > 0)
            {
                MobileJSONResponse response = JsonConvert.DeserializeObject<MobileJSONResponse>(vehicle.BufferedItems[0].ToString());

                _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME)
                        .Filter(new { id = response.id })
                        .Update(new { timeStamp = dtDateTime, isLive = true }).Run(_rethinkDbConnection);

                _rethinkDbSingleton.Db(DATABASE_NAME).Table(CORDINATE_TABLE_NAME).Insert(new Coordinates
                {
                    timeStamp = dtDateTime,
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

        public VehicleResponse GetAllVehicleByInstitutionId(IdleModel model)
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
            {
                oVehicleResponse.Status = false;
                oVehicleResponse.Message = "Institution does not exists in database.";
                oVehicleResponse.responseCode = ResponseCode.NotFound;
                oVehicleResponse.Data = null;
                return oVehicleResponse;
            }

            if (model.status == "active")
            {
                ReqlFunction1 filterForLive = expr => expr["isLive"].Eq(true);
                filterSerializedForLive = ReqlRaw.ToRawString(filterForLive);
            }
            else
            {
                ReqlFunction1 filterForLive = expr => expr["isLive"].Eq(false);
                filterSerializedForLive = ReqlRaw.ToRawString(filterForLive);
            }
            var filterExprForLive = ReqlRaw.FromRawString(filterSerializedForLive);

            if (!string.IsNullOrEmpty(Convert.ToString(model.startAt)) && !string.IsNullOrEmpty(Convert.ToString(model.endAt)) && DateTime.TryParse(Convert.ToString(model.startAt), out startDate) && DateTime.TryParse(Convert.ToString(model.endAt), out endDate))
            {
                ReqlFunction1 filterForStartDate = expr => expr["timeStamp"].Ge(startDate);
                string filterSerializedForStartDate = ReqlRaw.ToRawString(filterForStartDate);
                var filterExprForStartDate = ReqlRaw.FromRawString(filterSerializedForStartDate);

                ReqlFunction1 filterForEndDate = expr => expr["timeStamp"].Le(endDate);
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

                    if (((Newtonsoft.Json.Linq.JProperty)value).Name.ToString() == "id")
                    {

                        ReqlFunction1 cordinatefilter = expr => expr["mobileId"].Eq(((Newtonsoft.Json.Linq.JProperty)value).Value.ToString());
                        string cordinatefilterSerialized = ReqlRaw.ToRawString(cordinatefilter);
                        var cordinatefilterExpr = ReqlRaw.FromRawString(cordinatefilterSerialized);
                        Cursor<object> coordinates = _rethinkDbSingleton.Db(DATABASE_NAME).Table(CORDINATE_TABLE_NAME).Filter(cordinatefilterExpr).Run(_rethinkDbConnection);

                        foreach (var coordinate in coordinates)
                        {
                            string latitude = string.Empty, longitude = string.Empty, timeStamp = string.Empty;
                            foreach (var cordinatevalue in JObject.Parse(coordinate.ToString()).Children())
                            {

                                if (((Newtonsoft.Json.Linq.JProperty)cordinatevalue).Name.ToString() == "latitude")
                                {
                                    latitude = ((Newtonsoft.Json.Linq.JProperty)cordinatevalue).Value.ToString();
                                }
                                else if (((Newtonsoft.Json.Linq.JProperty)cordinatevalue).Name.ToString() == "longitude")
                                {
                                    longitude = ((Newtonsoft.Json.Linq.JProperty)cordinatevalue).Value.ToString();
                                }
                                else if (((Newtonsoft.Json.Linq.JProperty)cordinatevalue).Name.ToString() == "timeStamp")
                                {
                                    var epocTime = ((Newtonsoft.Json.Linq.JProperty)cordinatevalue).Value.ToString();
                                    foreach (var timeStampVal in JObject.Parse(epocTime).Children())
                                    {
                                        if (((Newtonsoft.Json.Linq.JProperty)timeStampVal).Name.ToString() == "epoch_time")
                                        {
                                            var UnixTime = ((Newtonsoft.Json.Linq.JProperty)timeStampVal).Value.ToString();

                                            System.DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                                            timeStamp = dateTime.AddSeconds(Convert.ToDouble(UnixTime)).ToLocalTime().ToString();
                                            break;
                                        }
                                    }
                                }
                                else if (((Newtonsoft.Json.Linq.JProperty)cordinatevalue).Name.ToString() == "deviceId")
                                {
                                    DeviceId = ((Newtonsoft.Json.Linq.JProperty)cordinatevalue).Value.ToString();
                                }
                            }
                            listCoordinates.Add(new CoordinatesDetail
                            {
                                latitude = Convert.ToDouble(latitude),
                                longitude = Convert.ToDouble(longitude),
                                timeStamp = timeStamp
                            });
                        }
                    }
                    else if (((Newtonsoft.Json.Linq.JProperty)value).Name.ToString() == "vehicleId")
                    {
                        VehicleId = ((Newtonsoft.Json.Linq.JProperty)value).Value.ToString();
                    }
                    else if (((Newtonsoft.Json.Linq.JProperty)value).Name.ToString() == "institutionId")
                    {
                        institutionId = ((Newtonsoft.Json.Linq.JProperty)value).Value.ToString();
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
            {
                oVehicleResponse.Status = false;
                oVehicleResponse.Message = "Vehicle does not exists in database.";
                oVehicleResponse.responseCode = ResponseCode.NotFound;
                oVehicleResponse.Data = null;
                return oVehicleResponse;
            }

            oVehicleResponse.Status = true;
            oVehicleResponse.Message = "Success";
            oVehicleResponse.responseCode = ResponseCode.Success;
            oVehicleResponse.Data = listVehicles;
            return oVehicleResponse;
        }

        public VehicleResponse GetAllVehicleDetail(IdleModel model)
        {
            var keys = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Run(_rethinkDbConnection);
            DateTime startDate;
            DateTime endDate;
            Cursor<object> vehicles;
            string filterSerialized = string.Empty;
            VehicleResponse oVehicleResponse = new VehicleResponse();

            if (model.status == "active")
            {
                ReqlFunction1 filter = expr => expr["isLive"].Eq(true);
                filterSerialized = ReqlRaw.ToRawString(filter);
            }
            else
            {
                ReqlFunction1 filter = expr => expr["isLive"].Eq(false);
                filterSerialized = ReqlRaw.ToRawString(filter);
            }
            var filterExpr = ReqlRaw.FromRawString(filterSerialized);

            if (!string.IsNullOrEmpty(Convert.ToString(model.startAt)) && !string.IsNullOrEmpty(Convert.ToString(model.endAt)) && DateTime.TryParse(Convert.ToString(model.startAt), out startDate) && DateTime.TryParse(Convert.ToString(model.endAt), out endDate))
            {
                ReqlFunction1 filterForStartDate = expr => expr["timeStamp"].Ge(startDate);
                string filterSerializedForStartDate = ReqlRaw.ToRawString(filterForStartDate);
                var filterExprForStartDate = ReqlRaw.FromRawString(filterSerializedForStartDate);

                ReqlFunction1 filterForEndDate = expr => expr["timeStamp"].Le(endDate);
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

            foreach (var vehicle in vehicles)
            {
                string VehicleId = string.Empty, institutionId = string.Empty, DeviceId = string.Empty;
                foreach (var value in JObject.Parse(vehicle.ToString()).Children())
                {

                    if (((Newtonsoft.Json.Linq.JProperty)value).Name.ToString() == "id")
                    {
                        ReqlFunction1 cordinatefilter = expr => expr["mobileId"].Eq(((Newtonsoft.Json.Linq.JProperty)value).Value.ToString());
                        string cordinatefilterSerialized = ReqlRaw.ToRawString(cordinatefilter);
                        var cordinatefilterExpr = ReqlRaw.FromRawString(cordinatefilterSerialized);
                        Cursor<object> coordinates = _rethinkDbSingleton.Db(DATABASE_NAME).Table(CORDINATE_TABLE_NAME).Filter(cordinatefilterExpr).Run(_rethinkDbConnection);

                        foreach (var coordinate in coordinates)
                        {
                            string latitude = string.Empty, longitude = string.Empty, timeStamp = string.Empty;
                            foreach (var cordinatevalue in JObject.Parse(coordinate.ToString()).Children())
                            {

                                if (((Newtonsoft.Json.Linq.JProperty)cordinatevalue).Name.ToString() == "latitude")
                                {
                                    latitude = ((Newtonsoft.Json.Linq.JProperty)cordinatevalue).Value.ToString();
                                }
                                else if (((Newtonsoft.Json.Linq.JProperty)cordinatevalue).Name.ToString() == "longitude")
                                {
                                    longitude = ((Newtonsoft.Json.Linq.JProperty)cordinatevalue).Value.ToString();
                                }
                                else if (((Newtonsoft.Json.Linq.JProperty)cordinatevalue).Name.ToString() == "timeStamp")
                                {
                                    var epocTime = ((Newtonsoft.Json.Linq.JProperty)cordinatevalue).Value.ToString();
                                    foreach (var timeStampVal in JObject.Parse(epocTime).Children())
                                    {
                                        if (((Newtonsoft.Json.Linq.JProperty)timeStampVal).Name.ToString() == "epoch_time")
                                        {
                                            var UnixTime = ((Newtonsoft.Json.Linq.JProperty)timeStampVal).Value.ToString();

                                            System.DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                                            timeStamp = dateTime.AddSeconds(Convert.ToDouble(UnixTime)).ToLocalTime().ToString();
                                            break;
                                        }
                                    }
                                }
                                else if (((Newtonsoft.Json.Linq.JProperty)cordinatevalue).Name.ToString() == "deviceId")
                                {
                                    DeviceId = ((Newtonsoft.Json.Linq.JProperty)cordinatevalue).Value.ToString();
                                }
                            }
                            listCoordinates.Add(new CoordinatesDetail
                            {
                                latitude = Convert.ToDouble(latitude),
                                longitude = Convert.ToDouble(longitude),
                                timeStamp = timeStamp
                            });
                        }
                    }
                    else if (((Newtonsoft.Json.Linq.JProperty)value).Name.ToString() == "vehicleId")
                    {
                        VehicleId = ((Newtonsoft.Json.Linq.JProperty)value).Value.ToString();
                    }
                    else if (((Newtonsoft.Json.Linq.JProperty)value).Name.ToString() == "institutionId")
                    {
                        institutionId = ((Newtonsoft.Json.Linq.JProperty)value).Value.ToString();
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
            {
                oVehicleResponse.Status = false;
                oVehicleResponse.Message = "Vehicle does not exists in database.";
                oVehicleResponse.responseCode = ResponseCode.NotFound;
                oVehicleResponse.Data = null;
                return oVehicleResponse;
            }

            oVehicleResponse.Status = true;
            oVehicleResponse.Message = "Success";
            oVehicleResponse.responseCode = ResponseCode.Success;
            oVehicleResponse.Data = listVehicles;
            return oVehicleResponse;
        }

        public string GetInstitutionId(string mobileId)
        {
            var vehicle = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Get(mobileId).Run(_rethinkDbConnection);
            string institutionId = string.Empty;

            foreach (var elements in vehicle)
            {
                if (((Newtonsoft.Json.Linq.JProperty)elements).Name == "institutionId")
                {
                    institutionId = ((Newtonsoft.Json.Linq.JProperty)elements).Value.ToString();
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
            ReqlFunction1 filter = expr => expr["timeStamp"].Le(DateTime.UtcNow.AddMinutes(-10));
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
                ReqlFunction1 filter = expr => expr["timeStamp"].Ge(DateTime.UtcNow.AddHours(-24));
                string filterSerialized = ReqlRaw.ToRawString(filter);
                var filterExpr = ReqlRaw.FromRawString(filterSerialized);
                Cursor<object> coordinates = _rethinkDbSingleton.Db(DATABASE_NAME).Table(CORDINATE_TABLE_NAME).Filter(filterExpr).Run(_rethinkDbConnection);

                List<ArchiveCoordinates> archiveCoordinates = new List<ArchiveCoordinates>();
                double latitude = 0, longitude = 0;
                int deviceId = 0, vehicleId = 0;
                string CoordinateId = string.Empty;
                DateTime? timeStamp = null;
                foreach (var coordinate in coordinates)
                {
                    foreach (var value in JObject.Parse(coordinate.ToString()).Children())
                    {
                        if (((Newtonsoft.Json.Linq.JProperty)value).Name.ToString() == "id")
                        {
                            CoordinateId = ((Newtonsoft.Json.Linq.JProperty)value).Value.ToString();
                        }
                        if (((Newtonsoft.Json.Linq.JProperty)value).Name.ToString() == "latitude")
                        {
                            latitude = Convert.ToDouble(((Newtonsoft.Json.Linq.JProperty)value).Value.ToString());
                        }
                        else if (((Newtonsoft.Json.Linq.JProperty)value).Name.ToString() == "longitude")
                        {
                            longitude = Convert.ToDouble(((Newtonsoft.Json.Linq.JProperty)value).Value.ToString());
                        }
                        else if (((Newtonsoft.Json.Linq.JProperty)value).Name.ToString() == "timeStamp")
                        {
                            var epocTime = ((Newtonsoft.Json.Linq.JProperty)value).Value.ToString();
                            foreach (var timeStampVal in JObject.Parse(epocTime).Children())
                            {
                                if (((Newtonsoft.Json.Linq.JProperty)timeStampVal).Name.ToString() == "epoch_time")
                                {
                                    var UnixTime = ((Newtonsoft.Json.Linq.JProperty)timeStampVal).Value.ToString();

                                    System.DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                                    timeStamp = dateTime.AddSeconds(Convert.ToDouble(UnixTime)).ToLocalTime();
                                    break;
                                }
                            }
                        }
                        else if (((Newtonsoft.Json.Linq.JProperty)value).Name.ToString() == "deviceId")
                        {
                            deviceId = Convert.ToInt32(((Newtonsoft.Json.Linq.JProperty)value).Value.ToString());
                        }
                        else if (((Newtonsoft.Json.Linq.JProperty)value).Name.ToString() == "mobileId")
                        {
                            var vehicle = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Get(((Newtonsoft.Json.Linq.JProperty)value).Value.ToString()).Run(_rethinkDbConnection);
                            foreach (var elements in vehicle)
                            {
                                if (((Newtonsoft.Json.Linq.JProperty)elements).Name == "vehicleId")
                                {
                                    vehicleId = Convert.ToInt32(((Newtonsoft.Json.Linq.JProperty)elements).Value.ToString());
                                    break;
                                }
                            }
                        }
                    }

                    archiveCoordinates.Add(new ArchiveCoordinates
                    {
                        CoordinateId = CoordinateId,
                        Latitude = latitude,
                        Longitude = longitude,
                        Timestamp = timeStamp.ToString(),
                        VehicleId = vehicleId,
                        DeviceId = deviceId
                    });
                }

                if (archiveCoordinates.Count > 0 && archiveCoordinates != null)
                {
                    using (var client = new HttpClient())
                    {
                        client.BaseAddress = new Uri(_appSettings.Host + _dependencies.ArchiveTrackServiceUrl);
                        var res = client.PostAsync("feeds", new StringContent(new JavaScriptSerializer().Serialize(archiveCoordinates), Encoding.UTF8, "application/json")).Result;

                        if (res.StatusCode == HttpStatusCode.Created)
                        {
                            _rethinkDbSingleton.Db(DATABASE_NAME).Table(CORDINATE_TABLE_NAME).Filter(filterExpr).Delete().Run(_rethinkDbConnection);
                        }
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
            ReqlFunction1 filter = expr => expr["timeStamp"].Ge(DateTime.UtcNow.AddDays(-7));
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
                        timeStamp = DateTime.UtcNow
                    }).Run(_rethinkDbConnection);
                }).Wait();
            }

            return Task.CompletedTask;
        }

        public string GetVehicleId(string mobileId)
        {
            var vehicle = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Get(mobileId).Run(_rethinkDbConnection);
            string vehicleId = string.Empty;

            foreach (var elements in vehicle)
            {
                if (((Newtonsoft.Json.Linq.JProperty)elements).Name == "vehicleId")
                {
                    vehicleId = ((Newtonsoft.Json.Linq.JProperty)elements).Value.ToString();
                    break;
                }
            }
            return vehicleId;

        }
    }
}