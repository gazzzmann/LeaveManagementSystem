using Humanizer;
using LeaveManagementSystem.Web.Data;
using LeaveManagementSystem.Web.Models.LeaveAllocations;
using LeaveManagementSystem.Web.Services.Periods;
using LeaveManagementSystem.Web.Services.Users;

namespace LeaveManagementSystem.Web.Services.LeaveAllocations;

public class LeaveAllocationsService(ApplicationDbContext _context, IUserService _userService, IMapper _mapper, IPeriodsService _periodsService) : ILeaveAllocationsService
{
    public async Task AllocateLeave(string employeeId)
    {
        //get all leave types
        var leaveTypes = await _context.LeaveTypes
            .Where(q => !q.LeaveAllocations.Any(x => x.EmployeeId == employeeId))
            .ToListAsync();

        //get period based on current year
        var period = await _periodsService.GetCurrentPeriod();
        var monthsRemaining = (period.EndDate.Month - DateTime.Now.Month) + 1;

        //foreach leave type, create an allocation entry
        foreach (var leaveType in leaveTypes)
        {
           
            var leaveAllocation = new LeaveAllocation
            {
                EmployeeId = employeeId, // Which employee it’s for
                LeaveTypeId = leaveType.Id, //What type of leave it is
                PeriodId = period.Id,  //  Which period or year it belongs to
                Days = (leaveType.NumberOfDays / 12) * monthsRemaining //how many days the employee should ge
            };

            _context.LeaveAllocations.Add(leaveAllocation); 
        }

        await _context.SaveChangesAsync();
        //Then Inject into the register page
    }

    public async Task<EmployeeAllocationVM> GetEmployeeAllocations(string? userId)
    {
        var user = string.IsNullOrEmpty(userId)
            ? await _userService.GetLoggedInUser()
            : await _userService.GetUserById(userId);

        var allocations = await GetAllocations(user.Id);
        var allocationVmList = _mapper.Map<List<LeaveAllocation>, List<LeaveAllocationVM>>(allocations);
        var leaveTypesCount = await _context.LeaveTypes.CountAsync();

        var employeeVm = new EmployeeAllocationVM
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            Email = user.Email,
            LeaveAllocations = allocationVmList,
            IsCompletedAllocation = leaveTypesCount == allocations.Count
        };

        return employeeVm;
    }

    public async Task<List<EmployeeListVM>> GetEmployees()
    {
        var users = await _userService.GetEmployees();
        var employees = _mapper.Map<List<ApplicationUser>, List<EmployeeListVM>>(users.ToList());

        return employees;
    }

    public async Task<LeaveAllocationEditVM> GetEmployeeAllocation(int allocationId)
    {
        var allocation = await _context.LeaveAllocations
            .Include(q => q.LeaveType)
            .Include(q => q.Employee)
            .FirstOrDefaultAsync(Queryable => Queryable.Id == allocationId);

        var model = _mapper.Map<LeaveAllocationEditVM>(allocation);
        return model;
                           
    }

    public async Task EditAllocation(LeaveAllocationEditVM allocationEditVm)
    {
        //var leaveAllocation = await GetEmployeeAllocation(allocationEditVm.Id);
        //if (leaveAllocation == null)
        //{
        //    throw new Exception("Leave allocation record does not exist");
        //}
        //leaveAllocation.Days = allocationEditVm.Days;
        //_context.LeaveAllocations.Update(leaveAllocation);   OR

       await _context.LeaveAllocations
            .Where(q => q.Id == allocationEditVm.Id)
            .ExecuteUpdateAsync(q => q.SetProperty(x => x.Days, x => allocationEditVm.Days));
    }

    public async Task<LeaveAllocation> GetCurrentAllocation(int leaveTypeId, string employeeId)
    {
        var period = await _periodsService.GetCurrentPeriod();
        var allocation = await _context.LeaveAllocations
            .FirstAsync(q => q.EmployeeId == employeeId
                                      && q.LeaveTypeId == leaveTypeId
                                      && q.PeriodId == period.Id);
        return allocation;
    }

    private async Task<List<LeaveAllocation>> GetAllocations(string? userId)
    {

        var period = await _periodsService.GetCurrentPeriod();
        var leavAllocations = await _context.LeaveAllocations
            .Include(q => q.LeaveType)
            .Include(q => q.Period)
            .Where(q => q.EmployeeId == userId && q.Period.Id == period.Id)
            .ToListAsync();

        return leavAllocations;
    }

    private async Task<bool> AllocationExists(string userId, int leaveTypeId, int periodId)
    {
        var exists = await _context.LeaveAllocations
            .AnyAsync(q => q.EmployeeId == userId
                           && q.LeaveTypeId == leaveTypeId
                           && q.PeriodId == periodId
                           );
        return exists;
    }

   
}


