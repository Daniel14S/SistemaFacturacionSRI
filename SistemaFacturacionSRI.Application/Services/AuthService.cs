using System;
using System.Collections.Generic;
using System.Linq;
using SistemaFacturacionSRI.Application.DTOs.Auth;
using SistemaFacturacionSRI.Application.Interfaces;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Application.Interfaces.Security;
using SistemaFacturacionSRI.Application.Security;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Application.Services
{
    public class AuthService : IAuthService
    {
        private const int MaxIntentosFallidos = 5;

        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly JwtTokenGenerator _jwtTokenGenerator;

        public AuthService(
            IUsuarioRepository usuarioRepository,
            IPasswordHasher passwordHasher,
            JwtTokenGenerator jwtTokenGenerator)
        {
            _usuarioRepository = usuarioRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var usuario = await _usuarioRepository.ObtenerPorUsernameAsync(request.Username);
            if (usuario == null)
            {
                return CredencialesInvalidas();
            }

            if (!usuario.Estado)
            {
                return Bloqueado();
            }

            if (usuario.IntentosLogin >= MaxIntentosFallidos)
            {
                usuario.Estado = false;
                await _usuarioRepository.ActualizarAsync(usuario);
                return Bloqueado();
            }

            var passwordValido = _passwordHasher.VerifyPassword(request.Password, usuario.PasswordHash);
            if (!passwordValido)
            {
                usuario.IntentosLogin += 1;
                if (usuario.IntentosLogin >= MaxIntentosFallidos)
                {
                    usuario.Estado = false;
                }

                await _usuarioRepository.ActualizarAsync(usuario);

                return usuario.Estado
                    ? CredencialesInvalidas(MaxIntentosFallidos - usuario.IntentosLogin)
                    : Bloqueado();
            }

            usuario.IntentosLogin = 0;
            usuario.UltimoAcceso = DateTime.UtcNow;
            await _usuarioRepository.ActualizarAsync(usuario);

            var token = _jwtTokenGenerator.GenerateToken(
                usuario.UsuarioId,
                usuario.Username,
                usuario.Email,
                usuario.Rol?.NombreRol ?? string.Empty);

            var expiresAt = _jwtTokenGenerator.GetExpirationDate(token);

            return new LoginResponseDto
            {
                Success = true,
                Message = "Inicio de sesión exitoso",
                Token = token,
                Usuario = MapearUsuario(usuario),
                ExpiresAt = expiresAt
            };
        }

        public async Task<bool> LogoutAsync(int usuarioId)
        {
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);
            if (usuario == null)
            {
                return false;
            }

            usuario.UltimoAcceso = DateTime.UtcNow;
            await _usuarioRepository.ActualizarAsync(usuario);
            return true;
        }

        public Task<bool> ValidarTokenAsync(string token)
        {
            var valido = _jwtTokenGenerator.ValidateToken(token);
            return Task.FromResult(valido);
        }

        public Task<int?> ObtenerUsuarioIdDesdeTokenAsync(string token)
        {
            var userId = _jwtTokenGenerator.GetUsuarioIdFromToken(token);
            return Task.FromResult(userId);
        }

        private static LoginResponseDto CredencialesInvalidas(int intentosRestantes = MaxIntentosFallidos)
        {
            return new LoginResponseDto
            {
                Success = false,
                Message = intentosRestantes < MaxIntentosFallidos
                    ? $"Credenciales inválidas. Intentos restantes: {intentosRestantes}."
                    : "Usuario o contraseña incorrectos."
            };
        }

        private static LoginResponseDto Bloqueado()
        {
            return new LoginResponseDto
            {
                Success = false,
                Message = "Usuario bloqueado por múltiples intentos fallidos. Contacte al administrador."
            };
        }

        private static UsuarioDto MapearUsuario(Usuario usuario)
        {
            return new UsuarioDto
            {
                Id = usuario.UsuarioId,
                Username = usuario.Username,
                Email = usuario.Email,
                Rol = usuario.Rol?.NombreRol ?? string.Empty,
                Estado = usuario.Estado,
                FechaCreacion = usuario.FechaCreacion,
                UltimoAcceso = usuario.UltimoAcceso,
                NombreCompleto = ConstruirNombreCompleto(usuario)
            };
        }

        private static string? ConstruirNombreCompleto(Usuario usuario)
        {
            var nombres = new List<string?>
            {
                usuario.Nombre1,
                usuario.Nombre2,
                usuario.Apellido1,
                usuario.Apellido2
            };

            var nombreCompleto = string.Join(" ", nombres.Where(n => !string.IsNullOrWhiteSpace(n)));
            return string.IsNullOrWhiteSpace(nombreCompleto) ? null : nombreCompleto;
        }
    }
}
