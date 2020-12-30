using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TrackService.RethinkDb_Abstractions;

namespace TrackService.RethinkDb_Changefeed.DataAccess.Abstraction
{
    public interface IDataAccessRepository
    {
        Task EnsureDatabaseCreated();
        Task InsertCordinates(CordinatesModel trackingStats);
        Task<IChangefeed<Coordinates>> GetCoordinatesChangeFeedback(CancellationToken cancellationToken);
        dynamic GetVehicles(string vehicleId, Pagination pageInfo, IdleModel idleModel);
        string GetInstitutionId(string mobileId);
        bool CheckVehicleExists(string vehicleId);
        bool CheckInstitutionExists(string institutionId);
        List<string> UpdateVehicleStatus();
        void ChangeVehicleStatus(string vehicleId);
        void SyncCoordinatesToArchiveTable();
        void SyncVehiclesToArchiveTable();
        Task InsertMobiles(MobilesModel mobileModel);
        bool SuperInstitutions(string tokenInstitutionId);
        int IdDecryption(string id);
        string IdEncryption(int id);
        bool CheckVehicleByInstitutionExists(string vehicleId, string institutionId);
        dynamic ClearLiveTrackingDatabase(string vehicleId);
        string GetVehicleId(string mobileId);
    }
}
