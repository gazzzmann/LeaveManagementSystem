using LeaveManagementSystem.Web.Data;

namespace LeaveManagementSystem.Web.MappingProfiles
{
    public class LeaveTypeAutoMapperProfile : Profile

    {
        public LeaveTypeAutoMapperProfile()
        {
            // Example mapping configuration
            CreateMap<LeaveType, LeaveTypeReadOnlyVM>();     
            CreateMap<LeaveTypeCreateVM, LeaveType>();
            CreateMap<LeaveTypeEditVM, LeaveType>().ReverseMap();
        }
    }
}
