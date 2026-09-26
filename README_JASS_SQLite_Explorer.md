# JASS SQLite Explorer

**A practical C#/.NET desktop SQLite database browser, editor and inspection tool.**

JASS SQLite Explorer is a Windows desktop application built with **C#**, **.NET 8**, **WinForms**, and **Microsoft.Data.Sqlite**.

It started as a focused C# learning project and is now a useful standalone SQLite utility. It is also the foundation for the broader **JASS Legacy Modernization** direction, where older database systems such as DBF/FoxPro can eventually be analyzed and migrated to modern database platforms.

---

## Features

### Database Management

- Create a new SQLite database
- Open existing `.db`, `.sqlite`, and `.sqlite3` files
- Close the current database
- Refresh database schema
- Display database information
- Run SQLite `VACUUM`
- Run SQLite `ANALYZE`

### Schema Explorer

The left-hand schema tree provides access to:

- Tables
- Views
- Indexes
- Triggers

### Data Browser

- Browse table records
- Display row and column counts
- Resizable columns
- Full-width data grid
- Search/filter visible records
- Select individual or multiple rows
- Copy selected cells
- `Ctrl+C` keyboard shortcut
- Unicode and multilingual data support

### Table Structure

The Structure view displays:

- Column name
- Data type
- NOT NULL
- Default value
- Primary key status

### Data Editing

For ordinary SQLite rowid tables:

- Add rows
- Edit rows
- Delete one or more rows
- Delete confirmation
- Transaction-based multi-row deletion
- NULL values supported in the editor

### SQL Workspace

- SQL query editor
- Execute SQL commands
- Display query results
- Display affected-row counts
- SQL history
- Up to 50 recent queries
- Clear SQL editor

### Export

Export the current result/data grid to:

- CSV
- JSON

---

## Technology Stack

| Component | Technology |
|---|---|
| Language | C# |
| Framework | .NET 8 |
| GUI | Windows Forms |
| Database | SQLite |
| SQLite provider | Microsoft.Data.Sqlite |
| Data handling | ADO.NET / DataTable / BindingSource |
| Serialization | System.Text.Json |
| Platform | Windows |

---

## Screenshots

The application has a simple database-tool layout:

```text
┌─────────────────────────────────────────────────────────────┐
│ JASS SQLite Explorer                                       │
├─────────────────────────────────────────────────────────────┤
│ New  Open  Refresh  Add Row  Edit  Delete  SQL  Export     │
├───────────────┬─────────────────────────────────────────────┤
│ Database      │                                             │
│ Schema        │              Data Browser                   │
│               │                                             │
│ Tables        │   ┌─────────────────────────────────────┐   │
│  ├ table1     │   │ records                             │   │
│  ├ table2     │   │                                     │   │
│               │   └─────────────────────────────────────┘   │
│ Views         │                                             │
│ Indexes       │              Structure                      │
│ Triggers      │                                             │
├───────────────┴─────────────────────────────────────────────┤
│ SQL Query                                                    │
│ SELECT ...                                                   │
├─────────────────────────────────────────────────────────────┤
│ Status                                                       │
└─────────────────────────────────────────────────────────────┘
```

---

## Getting Started

### Requirements

- Windows 10/11
- .NET 8 SDK

Download the .NET SDK from Microsoft:

https://dotnet.microsoft.com/download/dotnet/8.0

### Clone the repository

```powershell
git clone https://github.com/Fanu2/JASS-SQLite-Explorer.git
cd JASS-SQLite-Explorer
```

### Restore packages

```powershell
dotnet restore
```

### Build

```powershell
dotnet build
```

### Run

```powershell
dotnet run
```

---

## First Test

After launching the application:

1. Choose **File → Open Database**
2. Select an SQLite database.
3. Select a table from the schema tree.
4. Browse the records.
5. Try the search box.
6. Open **Structure** to inspect columns.
7. Use the SQL editor.

For example:

```sql
SELECT *
FROM your_table
LIMIT 20;
```

You can also try:

```sql
SELECT name, type
FROM sqlite_master
WHERE type IN ('table', 'view')
ORDER BY name;
```

---

## C# Learning Project

JASS SQLite Explorer is also intentionally designed as a practical C# learning project.

It demonstrates:

- Classes
- Records
- Methods
- Events
- WinForms controls
- Layout containers
- Dialogs
- Data binding
- `DataTable`
- `BindingSource`
- SQLite connections
- Parameterized SQL
- Transactions
- CRUD operations
- Exception handling
- Clipboard operations
- CSV generation
- JSON serialization

Instead of learning C# entirely through isolated exercises, the project uses a real application to introduce progressively more advanced concepts.

---

## Development Roadmap

### Completed

**v1.0**
- Open/create SQLite database
- Browse tables
- View records
- SQL execution
- Database information

**v1.1**
- Tables, views, indexes and triggers
- Table structure inspection
- Search
- CSV/JSON export
- SQL history

**v1.2**
- Add records
- Edit records
- Delete records
- Multi-row deletion
- Copy selected cells
- Improved data grid
- VACUUM / ANALYZE

### Future Ideas

**v1.3 — Schema Intelligence**
- Foreign-key inspection
- Detailed index information
- Trigger definitions
- View definitions
- `CREATE TABLE` SQL generation
- Relationship visualization

**v1.4 — Data Tools**
- Advanced filtering
- Column sorting controls
- Pagination for very large tables
- NULL-aware editing
- Better type-aware editors
- Import CSV

**Future — Legacy Modernization**

The project can eventually become part of:

> **JASS Legacy Modernization Studio**

with modules for:

```text
DBF / dBASE
     ↓
FoxPro / Visual FoxPro
     ↓
Schema Analysis
     ↓
Data Validation
     ↓
Migration Mapping
     ↓
SQLite / PostgreSQL
     ↓
Migration Report
```

---

## Design Philosophy

JASS SQLite Explorer follows a simple philosophy:

**Useful software first. Learning through building.**

The application is intentionally focused rather than overloaded with features. Each version introduces practical capabilities while providing an opportunity to learn C# and .NET through a working application.

---

## Project Direction

JASS SQLite Explorer is part of the broader JASS software portfolio and complements Python-based tools such as:

- PySide6 desktop applications
- SQLite utilities
- Document processing tools
- Language/corpus applications
- Data migration tools
- Legacy software modernization utilities

The long-term goal is to combine:

```text
Python
   +
C# / .NET
   +
SQL
   +
Legacy Systems Knowledge
```

into a practical toolkit for modernizing older business applications and databases.

---

## License

Choose an appropriate open-source license before publishing the repository.

For a permissive software project, **MIT License** is one possible choice.

---

## Status

**Current baseline: v1.2**

The application is functional and suitable for continued development.

