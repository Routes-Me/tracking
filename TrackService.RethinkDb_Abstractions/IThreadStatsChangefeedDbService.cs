using System.Threading;
using System.Threading.Tasks;

namespace TrackService.RethinkDb_Abstractions
{
    public interface ICoordinateChangeFeedbackBackgroundService
    {
        Task EnsureDatabaseCreated();
        Task InsertCordinates(CordinatesModel trackingStats);
        void UpdateVehicleStatus();
        void ChangeVehicleStatus(string vehicleId);
        string GetInstitutionId(string mobileId);
        bool CheckVehicleExists(string vehicleId);
        bool CheckInstitutionExists(string vehicleId);
        Task<IChangefeed<Coordinates>> GetCoordinatesChangeFeedback(CancellationToken cancellationToken);
        Task<dynamic> GetAllVehicleByInstitutionId(IdleModel IdleModel);
        Task<dynamic> GetAllVehicleDetail(Pagination pageInfo, IdleModel IdleModel);
        void SyncCoordinatesToArchiveTable();
        void SyncVehiclesToArchiveTable();
        Task InsertMobiles(MobilesModel trackingStats);
        string GetVehicleId(string vehicleId);
    }
}