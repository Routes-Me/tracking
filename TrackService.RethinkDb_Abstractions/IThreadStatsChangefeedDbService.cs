using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TrackService.RethinkDb_Abstractions
{
    public interface ICoordinateChangeFeedbackBackgroundService
    {
        Task EnsureDatabaseCreated();
        Task InsertCordinates(CordinatesModel trackingStats);
        List<string> UpdateVehicleStatus();
        void ChangeVehicleStatus(string vehicleId);
        string GetInstitutionId(string mobileId);
        bool CheckVehicleExists(string vehicleId);
        bool CheckInstitutionExists(string institutionId);
        Task<IChangefeed<Coordinates>> GetCoordinatesChangeFeedback(CancellationToken cancellationToken);
        dynamic GetVehicles(string vehicleId, Pagination pageInfo, IdleModel IdleModel);
        void SyncCoordinatesToArchiveTable();   
        void SyncVehiclesToArchiveTable();
        Task InsertMobiles(MobilesModel trackingStats);
        string GetVehicleId(string vehicleId);
        bool SuperInstitutions(string tokenInstitutionId);
        int IdDecryption(string id);
        string IdEncryption(int id);
        bool CheckVehicleByInstitutionExists(string vehicleId, string institutionId);
        Task<dynamic> ClearLiveTrackingDatabase(string vehicleId);
    }
}