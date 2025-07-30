using MedChain_BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedChain_BLL.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDTO> Login(LoginDTO loginDTO);
        Task<AuthResponseDTO> Register(RegisterDTO registerDTO);
        Task<bool> UserExists(string email);
    }
}
