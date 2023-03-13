using System;
using System.Text;
using ITBees.Models.Languages;

namespace ITBees.UserManager.DbMigrations
{
    public class LanguageDbMigration
    {
        public string GetInsertSqlQuerForAllLanaguages()
        {
            var sb = new StringBuilder();
            foreach (var type in InheritedMapper.BaseClassHelper.GetAllDerivedClassesFromBaseClass(typeof(Language)))
            {
                var instance = Activator.CreateInstance(type) as Language;
                sb.AppendLine($"INSERT INTO Language (Id, Code, Name, LanguageType) VALUES ('{instance.Id}', '{instance.Code}','{instance.Name}', '{instance.GetType().Name}Type' ON DUPLICATE KEY UPDATE Name=Name; ");
            }

            return sb.ToString();
        }
    }
}