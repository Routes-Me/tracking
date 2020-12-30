using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TrackService.RethinkDb_Abstractions;
using TrackService.RethinkDb_Changefeed.DataAccess.Abstraction;

namespace TrackService.RethinkDb_Changefeed
{
    public class ThreadStatsRethinkDbService : ICoordinateChangeFeedbackBackgroundService
    {
        private readonly IDataAccessRepository _dataAccessRepository;
        public ThreadStatsRethinkDbService(IDataAccessRepository dataAccessRepository)
        {
            _dataAccessRepository = dataAccessRepository;
        }

        public Task EnsureDatabaseCreated()
        {
            try
            {
                return _dataAccessRepository.EnsureDatabaseCreated();
            }
            catch (Exception ex)
            {
                return ReturnResponse.ExceptionResponse(ex);
            }
        }

        public Task InsertCordinates(CordinatesModel trackingStats)
        {
            try
            {
                return _dataAccessRepository.InsertCordinates(trackingStats);
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

        public dynamic GetVehicles(string vehicleId, Pagination pageInfo, IdleModel idleModel)
        {
            try
            {
                return _dataAccessRepository.GetVehicles(vehicleId, pageInfo, idleModel);
            }
            catch (Exception ex)
            {
                return ReturnResponse.ExceptionResponse(ex);
            }
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

        public List<string> UpdateVehicleStatus()
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

        public dynamic ClearLiveTrackingDatabase(string vehicleId)
        {
            try
            {
                return _dataAccessRepository.ClearLiveTrackingDatabase(vehicleId);
            }
            catch (Exception ex)
            {
                return ReturnResponse.ExceptionResponse(ex);
            }
        }
    }
}