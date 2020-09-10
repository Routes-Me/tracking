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
        VehicleResponse GetAllVehicleByInstitutionId(IdleModel IdleModel);
        VehicleResponse GetAllVehicleDetail(IdleModel IdleModel);
        void SyncCoordinatesToArchiveTable();
        void SyncVehiclesToArchiveTable();
        Task InsertMobiles(MobilesModel trackingStats);
        string GetVehicleId(string vehicleId);
    }
}