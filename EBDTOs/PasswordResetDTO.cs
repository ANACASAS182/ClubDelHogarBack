using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBDTOs
{
    public class PasswordResetDTO
    {
        public required string NewPassword { get; set; }
        public required string ConfirmNewPassword { get; set; }
        public required string Token { get; set; }

    }
}
