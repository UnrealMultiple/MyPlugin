using System.Data;

namespace TShockAPI.DB;

public static class TShockExt
{
    public static SqlTableCreator GetTableCreator(this IDbConnection db) => new(db, db.GetSqlQueryBuilder());
}
