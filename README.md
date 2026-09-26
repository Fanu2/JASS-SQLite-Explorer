# JASS SQLite Explorer v1.2

JASS SQLite Explorer v1.2 is a C#/.NET WinForms SQLite utility and a practical learning project.

## v1.2 additions

- Improved data grid with full-width columns
- Add row
- Edit selected row
- Delete selected rows with confirmation
- Transaction-based multi-row delete
- Copy selected cells / Ctrl+C
- Search/filter
- Table structure inspector
- CSV and JSON export
- SQL history
- Database information
- VACUUM and ANALYZE maintenance commands
- Tables, views, indexes and triggers schema tree

## Build

Use a clean project folder. Do not copy these files over another JASS SQLite Explorer project.

```powershell
dotnet restore
dotnet build
dotnet run
```

## Important editing note

The Add/Edit dialog treats an empty field as SQL NULL. SQLite rowid is used internally for update/delete operations. This works for ordinary rowid tables; WITHOUT ROWID tables are not targeted by the editing feature in this version.

## Learning focus

v1.2 introduces practical C# concepts including:
- classes and records
- WinForms events
- DataTable and BindingSource
- transactions
- parameterized SQL
- CRUD operations
- dialogs
- clipboard access
- JSON serialization
- exception handling
