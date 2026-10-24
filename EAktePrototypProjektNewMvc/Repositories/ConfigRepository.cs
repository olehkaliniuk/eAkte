using DreiF.Shared.Abstractions;
using EAktePrototypProjektNewMvc.Controllers;
using EAktePrototypProjektNewMvc.Exceptions;
using EAktePrototypProjektNewMvc.Models.Config;
using Microsoft.Data.SqlClient;
using System.Data;

namespace EAktePrototypProjektNewMvc.Repositories
{
    public class ConfigRepository
    {
        private readonly string _connectionString;
        private readonly ICurrentMandantProvider _mandantProvider;

        public ConfigRepository(string connectionString, ICurrentMandantProvider mandantProvider)
        {
            _connectionString = connectionString;
            _mandantProvider = mandantProvider;
        }

        public Config LoadConfig()
        {
            try
            {
                var config = new Config();

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                                  SELECT [KeyBezeichnung], [StringValue], [IntValue], [BoolValue], [DateTimeValue]
                                  FROM [Config]
                                  WHERE [IstAktiv] = 1
                                  AND MandantId = 999";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string key = reader["KeyBezeichnung"].ToString().Trim();
                    var prop = typeof(Config).GetProperty(key);
                    if (prop != null)
                    {
                        object value = null;
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                        switch (Type.GetTypeCode(targetType))
                        {
                            case TypeCode.String:
                                value = reader["StringValue"] != DBNull.Value ? reader["StringValue"].ToString() : null;
                                break;
                            case TypeCode.Int32:
                                value = reader["IntValue"] != DBNull.Value ? (int?)Convert.ToInt32(reader["IntValue"]) : null;
                                break;
                            case TypeCode.Boolean:
                                value = reader["BoolValue"] != DBNull.Value ? (bool?)Convert.ToBoolean(reader["BoolValue"]) : null;
                                break;
                            case TypeCode.DateTime:
                                value = reader["DateTimeValue"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DateTimeValue"]) : null;
                                break;
                        }

                        if (value != null)
                            prop.SetValue(config, value);
                    }
                }

                return config;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Fehler beim Laden des Configs.", ex);
            }
        }
    }
}