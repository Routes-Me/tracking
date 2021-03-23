using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrackService.RethinkDb_Abstractions;
using TrackService.RethinkDb_Changefeed.DataAccess.Abstraction;
using TrackService.RethinkDb_Changefeed.Model.Common;

namespace TrackService.RethinkDb_Changefeed
{
    public class ThreadStatsRethinkDbService : ICoordinateChangeFeedbackBackgroundService
    {
        private readonly IDataAccessRepository _dataAccessRepository;

        private const string DATABASE_NAME = "trackingdb";
        private const string MOBILE_TABLE_NAME = "mobiles";
        private const string CORDINATE_TABLE_NAME = "coordinates";
        public static bool IsAnotherServiceWorking = false;
        private readonly RethinkDb.Driver.RethinkDB _rethinkDbSingleton;
        private readonly Connection _rethinkDbConnection;
        private readonly AppSettings _appSettings;
        private readonly Dependencies _dependencies;


        public ThreadStatsRethinkDbService(IRethinkDbSingletonProvider rethinkDbSingletonProvider, IOptions<AppSettings> appSettings, IOptions<Dependencies> dependencies, IDataAccessRepository dataAccessRepository)
        {
            if (rethinkDbSingletonProvider == null)
            {
                throw new ArgumentNullException(nameof(rethinkDbSingletonProvider));
            }

            _rethinkDbSingleton = rethinkDbSingletonProvider.RethinkDbSingleton;
            _rethinkDbConnection = rethinkDbSingletonProvider.RethinkDbConnection;
            _appSettings = appSettings.Value;
            _dependencies = dependencies.Value;
            _dataAccessRepository = dataAccessRepository;
        }

        public Task InsertCordinates(List<Location> locations, int institutionId, int vehicleId)
        {
            try
            {
                return _dataAccessRepository.InsertCordinates(locations, institutionId, vehicleId);
            }
            catch (Exception ex)
            {
                return ReturnResponse.ExceptionResponse(ex);
            }
        }

        public async Task<IChangefeed<Coordinates>> GetCoordinatesChangeFeedback(CancellationToken cancellationToken)
        {
            try
            {
                return await _dataAccessRepository.GetCoordinatesChangeFeedback(cancellationToken);
            }
            catch (Exception ex)
            {
                return ReturnResponse.ExceptionResponse(ex);
            }
        }

        public (List<VehicleDetails>, int) GetVehicles(string vehicleId, Pagination pageInfo, IdleModel idleModel)
        {


            var vehicles = GetVehiclesFromDb(vehicleId, pageInfo, idleModel);
            var vehicleList = GetVehicleList(vehicles.Item1);

            return (vehicleList, vehicles.Item2);

        }

        public string GetInstitutionId(string mobileId)
        {
            try
            {
                return _dataAccessRepository.GetInstitutionId(mobileId);
            }
            catch (Exception ex)
            {
                return ReturnResponse.ExceptionResponse(ex);
            }
        }

        public bool CheckVehicleExists(string vehicleId)
        {
            try
            {
                return _dataAccessRepository.CheckVehicleExists(vehicleId);
            }
            catch (Exception ex)
            {
                return ReturnResponse.ExceptionResponse(ex);
            }
        }

        public bool CheckInstitutionExists(string institutionId)
        {
            try
            {
                return _dataAccessRepository.CheckInstitutionExists(institutionId);
            }
            catch (Exception ex)
            {
                return ReturnResponse.ExceptionResponse(ex);
            }
        }

        public List<IdealVehicleResponse> UpdateVehicleStatus()
        {
            try
            {
                return _dataAccessRepository.UpdateVehicleStatus();
            }
            catch (Exception ex)
            {
                return ReturnResponse.ExceptionResponse(ex);
            }
        }

        public void ChangeVehicleStatus(string vehicleId)
        {
            try
            {
                _dataAccessRepository.ChangeVehicleStatus(vehicleId);
            }
            catch (Exception) { }
        }

        public void SyncCoordinatesToArchiveTable()
        {
            try
            {
                _dataAccessRepository.SyncCoordinatesToArchiveTable();
            }
            catch (Exception) { }
        }

        public void SyncVehiclesToArchiveTable()
        {
            try
            {
                _dataAccessRepository.SyncVehiclesToArchiveTable();
            }
            catch (Exception) { }
        }

        public Task InsertMobiles(MobilesModel mobileModel)
        {
            try
            {
                return _dataAccessRepository.InsertMobiles(mobileModel);
            }
            catch (Exception ex)
            {
                return ReturnResponse.ExceptionResponse(ex);
            }
        }

        public string GetVehicleId(string mobileId)
        {
            try
            {
                return _dataAccessRepository.GetVehicleId(mobileId);
            }
            catch (Exception ex)
            {
                return ReturnResponse.ExceptionResponse(ex);
            }
        }

        public bool SuperInstitutions(string tokenInstitutionId)
        {
            try
            {
                return _dataAccessRepository.SuperInstitutions(tokenInstitutionId);
            }
            catch (Exception ex)
            {
                return ReturnResponse.ExceptionResponse(ex);
            }
        }

        public int IdDecryption(string id)
        {
            try
            {
                return _dataAccessRepository.IdDecryption(id);
            }
            catch (Exception ex)
            {
                return ReturnResponse.ExceptionResponse(ex);
            }
        }

        public string IdEncryption(int id)
        {
            try
            {
                return _dataAccessRepository.IdEncryption(id);
            }
            catch (Exception ex)
            {
                return ReturnResponse.ExceptionResponse(ex);
            }
        }

        public bool CheckVehicleByInstitutionExists(string vehicleId, string institutionId)
        {
            try
            {
                return _dataAccessRepository.CheckVehicleByInstitutionExists(vehicleId, institutionId);
            }
            catch (Exception ex)
            {
                return ReturnResponse.ExceptionResponse(ex);
            }
        }

        public void ClearLiveTrackingDatabase(string vehicleId)
        {
            if (string.IsNullOrEmpty(vehicleId))
            {
                _rethinkDbSingleton.Db(DATABASE_NAME).Table(CORDINATE_TABLE_NAME).Delete().Run(_rethinkDbConnection);
                _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Delete().Run(_rethinkDbConnection);
            }
            else
            {
                string mobileId = string.Empty;
                ReqlFunction1 filterForVehicleId = expr => expr["vehicleId"].Eq(Convert.ToInt32(vehicleId));
                string filterSerializedForVehicleId = ReqlRaw.ToRawString(filterForVehicleId);
                var filterExprForVehicleId = ReqlRaw.FromRawString(filterSerializedForVehicleId);

                Cursor<object> VehicleData = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(filterExprForVehicleId).Run(_rethinkDbConnection);
                if (VehicleData.BufferedSize == 0)
                    throw new NullReferenceException("Vehicles does not exists in database.");

                foreach (var item in VehicleData)
                {
                    foreach (var value in JObject.Parse(item.ToString()).Children())
                    {
                        if (((JProperty)value).Name.ToString() == "id")
                        {
                            mobileId = ((JProperty)value).Value.ToString();
                        }
                    }
                    ReqlFunction1 filter = expr => expr["mobileId"].Eq(mobileId);
                    string filterSerialized = ReqlRaw.ToRawString(filter);
                    var filterExpr = ReqlRaw.FromRawString(filterSerialized);
                    _rethinkDbSingleton.Db(DATABASE_NAME).Table(CORDINATE_TABLE_NAME).Filter(filter).Delete().Run(_rethinkDbConnection);
                }
                _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(filterForVehicleId).Delete().Run(_rethinkDbConnection);
            }
        }

        private (Cursor<object>, int) GetVehiclesFromDb(string vehicleId, Pagination pageInfo, IdleModel idleModel)
        {
            DateTime startDate;
            DateTime endDate;
            Cursor<object> vehicles;
            long count = 0;
            DateTime.TryParse(Convert.ToString(idleModel.startAt), out startDate);
            DateTime.TryParse(Convert.ToString(idleModel.endAt), out endDate);

            if (!string.IsNullOrEmpty(idleModel.startAt) && !string.IsNullOrEmpty(idleModel.endAt))
            {
                ReqlFunction1 filterForStartDate = expr => expr["timestamp"].Ge(startDate);
                string filterSerializedForStartDate = ReqlRaw.ToRawString(filterForStartDate);
                var filterExprForStartDate = ReqlRaw.FromRawString(filterSerializedForStartDate);

                ReqlFunction1 filterForEndDate = expr => expr["timestamp"].Le(endDate);
                string filterSerializedForEndDate = ReqlRaw.ToRawString(filterForEndDate);
                var filterExprForEndDate = ReqlRaw.FromRawString(filterSerializedForEndDate);

                if (string.IsNullOrEmpty(idleModel.status))
                {
                    vehicles = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(filterForStartDate).Filter(filterForEndDate).Skip((pageInfo.offset - 1) * pageInfo.limit).Limit(pageInfo.limit)
                        .Run(_rethinkDbConnection);
                    count = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(filterForStartDate).Filter(filterForEndDate).Count().Run(_rethinkDbConnection);
                }
                else
                {
                    vehicles = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(new { isLive = false }).Filter(filterForStartDate).Filter(filterForEndDate).Skip((pageInfo.offset - 1) * pageInfo.limit)
                        .Limit(pageInfo.limit).Run(_rethinkDbConnection);
                    count = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(new { isLive = false }).Filter(filterForStartDate).Filter(filterForEndDate).Count().Run(_rethinkDbConnection);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(idleModel.status))
                {
                    vehicles = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Skip((pageInfo.offset - 1) * pageInfo.limit).Limit(pageInfo.limit).Run(_rethinkDbConnection);
                    count = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Count().Run(_rethinkDbConnection);
                }
                else
                {
                    vehicles = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(new { isLive = false }).Skip((pageInfo.offset - 1) * pageInfo.limit).Limit(pageInfo.limit).Run(_rethinkDbConnection);
                    count = _rethinkDbSingleton.Db(DATABASE_NAME).Table(MOBILE_TABLE_NAME).Filter(new { isLive = false }).Count().Run(_rethinkDbConnection);
                }
            }
            return (vehicles, Convert.ToInt32(count));
        }


        private List<VehicleDetails> GetVehicleList(Cursor<object> vehicles)
        {
            List<VehicleDetails> listVehicles = new List<VehicleDetails>();
            List<CoordinatesDetail> listCoordinates = new List<CoordinatesDetail>();
            foreach (var vehicle in vehicles)
            {
                string VehicleId = string.Empty, institutionId = string.Empty, DeviceId = string.Empty;
                foreach (var value in JObject.Parse(vehicle.ToString()).Children())
                {
                    var vehicleParamName = ((JProperty)value).Name.ToString();
                    var vehicleParamValue = ((JProperty)value).Value.ToString();
                    if (vehicleParamName == "id")
                    {
                        ReqlFunction1 cordinatefilter = expr => expr["mobileId"].Eq(vehicleParamValue);
                        string cordinatefilterSerialized = ReqlRaw.ToRawString(cordinatefilter);
                        var cordinatefilterExpr = ReqlRaw.FromRawString(cordinatefilterSerialized);
                        Cursor<object> coordinates = _rethinkDbSingleton.Db(DATABASE_NAME).Table(CORDINATE_TABLE_NAME).Filter(cordinatefilterExpr).Run(_rethinkDbConnection);

                        foreach (var coordinate in coordinates)
                        {
                            string latitude = string.Empty, longitude = string.Empty, timestamp = string.Empty;
                            foreach (var cordinatevalue in JObject.Parse(coordinate.ToString()).Children())
                            {
                                var coordinateParamName = ((JProperty)cordinatevalue).Name.ToString();
                                var coordinateParamValue = ((JProperty)cordinatevalue).Value.ToString();
                                if (coordinateParamName == "latitude")
                                {
                                    latitude = coordinateParamValue;
                                }
                                else if (coordinateParamName == "longitude")
                                {
                                    longitude = coordinateParamValue;
                                }
                                else if (coordinateParamName == "timestamp")
                                {
                                    var epocTime = coordinateParamValue;
                                    foreach (var timestampVal in JObject.Parse(epocTime).Children())
                                    {
                                        if (((JProperty)timestampVal).Name.ToString() == "epoch_time")
                                        {
                                            var UnixTime = ((JProperty)timestampVal).Value.ToString();
                                            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                                            timestamp = dateTime.AddSeconds(Convert.ToDouble(UnixTime)).ToLocalTime().ToString();
                                            break;
                                        }
                                    }
                                }
                                else if (coordinateParamName == "deviceId")
                                {
                                    DeviceId = coordinateParamValue;
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
                    else if (vehicleParamName == "vehicleId")
                    {
                        VehicleId = vehicleParamValue;
                    }
                    else if (vehicleParamName == "institutionId")
                    {
                        institutionId = vehicleParamValue;
                    }
                }
                List<CoordinatesDetail> coordinatesDetailsList = new List<CoordinatesDetail>();
                var latestCoodinates = listCoordinates.OrderByDescending(x => x.timestamp).FirstOrDefault();
                if (latestCoodinates != null)
                {
                    CoordinatesDetail coordinatesDetail = new CoordinatesDetail();
                    coordinatesDetail.latitude = latestCoodinates.latitude;
                    coordinatesDetail.longitude = latestCoodinates.longitude;
                    coordinatesDetail.timestamp = latestCoodinates.timestamp;
                    coordinatesDetailsList.Add(coordinatesDetail);
                }
                listVehicles.Add(new VehicleDetails
                {
                    deviceId = DeviceId,
                    vehicleId = VehicleId,
                    institutionId = institutionId,
                    coordinates = coordinatesDetailsList
                });
            }
            return listVehicles;
        }
    }
}