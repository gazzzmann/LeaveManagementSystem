using LeaveManagementSystem.Web.Models.LeaveAllocations;

namespace LeaveManagementSystem.Web.Services.LeaveAllocations
{
    public interface ILeaveAllocationsService
    {
        Task AllocateLeave(string employeeId);
        Task EditAllocation(LeaveAllocationEditVM allocationEditVm);
        Task<LeaveAllocation> GetCurrentAllocation(int leaveTypeId, string employeeId);
        Task<LeaveAllocationEditVM> GetEmployeeAllocation(int allocationId);
        Task<EmployeeAllocationVM> GetEmployeeAllocations(string? userId);
        Task<List<EmployeeListVM>> GetEmployees();
    }
}
