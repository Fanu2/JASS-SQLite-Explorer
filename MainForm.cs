using Microsoft.Data.Sqlite;
using System.Data;
using System.Text;
using System.Text.Json;

namespace JASS.SQLite.Explorer;

public class MainForm : Form
{
    private SqliteConnection? _db;
    private string? _path;
    private string? _currentTable;
    private DataTable? _currentData;
    private readonly BindingSource _source = new();
    private readonly TreeView _tree = new();
    private readonly DataGridView _grid = new();
    private readonly DataGridView _structureGrid = new();
    private readonly TextBox _sql = new();
    private readonly TextBox _search = new();
    private readonly Label _dbLabel = new();
    private readonly Label _status = new();
    private readonly ToolStripStatusLabel _rowStatus = new();
    private readonly TabControl _resultTabs = new();
    private readonly List<string> _history = new();

    public MainForm()
    {
        Text = "JASS SQLite Explorer v1.2";
        Width = 1400;
        Height = 850;
        MinimumSize = new Size(1100, 680);
        StartPosition = FormStartPosition.CenterScreen;
        BuildUi();
        SetStatus("Ready — open or create a SQLite database.");
    }

    private void BuildUi()
    {
        var menu = new MenuStrip();

        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add("New Database...", null, (_, _) => NewDatabase());
        file.DropDownItems.Add("Open Database...", null, (_, _) => OpenDatabase());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("Export Current Result...", null, (_, _) => ExportCurrent());
        file.DropDownItems.Add("Close Database", null, (_, _) => CloseDatabase());
        file.DropDownItems.Add("Exit", null, (_, _) => Close());
        menu.Items.Add(file);

        var database = new ToolStripMenuItem("&Database");
        database.DropDownItems.Add("Refresh Schema", null, (_, _) => LoadSchema());
        database.DropDownItems.Add("Database Information", null, (_, _) => ShowDatabaseInfo());
        database.DropDownItems.Add("VACUUM", null, (_, _) => RunMaintenance("VACUUM"));
        database.DropDownItems.Add("ANALYZE", null, (_, _) => RunMaintenance("ANALYZE"));
        menu.Items.Add(database);

        var sqlMenu = new ToolStripMenuItem("&SQL");
        sqlMenu.DropDownItems.Add("Execute", null, (_, _) => ExecuteSql());
        sqlMenu.DropDownItems.Add("SQL History", null, (_, _) => ShowHistory());
        sqlMenu.DropDownItems.Add("Clear Editor", null, (_, _) => _sql.Clear());
        menu.Items.Add(sqlMenu);

        var edit = new ToolStripMenuItem("&Edit");
        edit.DropDownItems.Add("Add Row", null, (_, _) => AddRow());
        edit.DropDownItems.Add("Edit Selected Row", null, (_, _) => EditSelectedRow());
        edit.DropDownItems.Add("Delete Selected Row", null, (_, _) => DeleteSelectedRows());
        edit.DropDownItems.Add(new ToolStripSeparator());
        edit.DropDownItems.Add("Copy Selected Cells", null, (_, _) => CopySelectedCells());
        menu.Items.Add(edit);

        MainMenuStrip = menu;
        Controls.Add(menu);

        var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
        AddButton(toolbar, "New", (_, _) => NewDatabase());
        AddButton(toolbar, "Open", (_, _) => OpenDatabase());
        AddButton(toolbar, "Refresh", (_, _) => LoadSchema());
        toolbar.Items.Add(new ToolStripSeparator());
        AddButton(toolbar, "Add Row", (_, _) => AddRow());
        AddButton(toolbar, "Edit Row", (_, _) => EditSelectedRow());
        AddButton(toolbar, "Delete", (_, _) => DeleteSelectedRows());
        toolbar.Items.Add(new ToolStripSeparator());
        AddButton(toolbar, "Execute SQL", (_, _) => ExecuteSql());
        AddButton(toolbar, "Export", (_, _) => ExportCurrent());
        AddButton(toolbar, "Copy", (_, _) => CopySelectedCells());
        Controls.Add(toolbar);

        _dbLabel.Dock = DockStyle.Fill;
        _dbLabel.Text = "No database open";
        _dbLabel.Padding = new Padding(8, 6, 8, 6);
        _dbLabel.Font = new Font(Font, FontStyle.Bold);
        _dbLabel.BackColor = Color.FromArgb(235, 240, 246);
        var dbPanel = new Panel { Dock = DockStyle.Top, Height = 36 };
        dbPanel.Controls.Add(_dbLabel);
        Controls.Add(dbPanel);

        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 285,
            FixedPanel = FixedPanel.Panel1
        };

        var left = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };
        left.Controls.Add(new Label
        {
            Text = "Database Schema",
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font(Font, FontStyle.Bold)
        });
        _tree.Dock = DockStyle.Fill;
        _tree.AfterSelect += (_, e) =>
        {
            if (e.Node?.Tag is SchemaNode n && n.Type is "table" or "view")
                SelectObject(n);
        };
        left.Controls.Add(_tree);
        mainSplit.Panel1.Controls.Add(left);

        var right = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 475
        };

        BuildResultTabs();
        right.Panel1.Controls.Add(_resultTabs);

        var sqlPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };
        var sqlHeader = new Panel { Dock = DockStyle.Top, Height = 38 };
        sqlHeader.Controls.Add(new Label
        {
            Text = "SQL Query",
            Dock = DockStyle.Left,
            Width = 100,
            Font = new Font(Font, FontStyle.Bold),
            Padding = new Padding(0, 7, 0, 0)
        });
        var execute = new Button { Text = "Execute SQL", Dock = DockStyle.Right, Width = 110 };
        execute.Click += (_, _) => ExecuteSql();
        sqlHeader.Controls.Add(execute);
        var clear = new Button { Text = "Clear", Dock = DockStyle.Right, Width = 75 };
        clear.Click += (_, _) => _sql.Clear();
        sqlHeader.Controls.Add(clear);

        _sql.Multiline = true;
        _sql.ScrollBars = ScrollBars.Both;
        _sql.AcceptsTab = true;
        _sql.Font = new Font("Consolas", 10);
        _sql.Dock = DockStyle.Fill;
        _sql.Text = "SELECT name, type FROM sqlite_master WHERE type IN ('table','view') ORDER BY name;";

        sqlPanel.Controls.Add(_sql);
        sqlPanel.Controls.Add(sqlHeader);
        right.Panel2.Controls.Add(sqlPanel);
        mainSplit.Panel2.Controls.Add(right);
        Controls.Add(mainSplit);

        var statusBar = new StatusStrip();
        statusBar.Items.Add(_rowStatus);
        _status.AutoSize = true;
        statusBar.Items.Add(new ToolStripControlHost(_status));
        Controls.Add(statusBar);

        menu.BringToFront();
        toolbar.BringToFront();
        dbPanel.BringToFront();
    }

    private void BuildResultTabs()
    {
        _resultTabs.Dock = DockStyle.Fill;

        var dataPage = new TabPage("Data");
        var dataPanel = new Panel { Dock = DockStyle.Fill };

        var searchPanel = new Panel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(5) };
        searchPanel.Controls.Add(new Label
        {
            Text = "Search:",
            Dock = DockStyle.Left,
            Width = 65,
            Padding = new Padding(0, 6, 0, 0)
        });

        _search.Dock = DockStyle.Fill;
        _search.PlaceholderText = "Search visible rows...";
        _search.TextChanged += (_, _) => ApplySearch();
        searchPanel.Controls.Add(_search);

        var clear = new Button { Text = "Clear", Dock = DockStyle.Right, Width = 70 };
        clear.Click += (_, _) => _search.Clear();
        searchPanel.Controls.Add(clear);

        var export = new Button { Text = "Export", Dock = DockStyle.Right, Width = 75 };
        export.Click += (_, _) => ExportCurrent();
        searchPanel.Controls.Add(export);

        ConfigureGrid(_grid);
        _grid.DataSource = _source;
        _grid.SelectionChanged += (_, _) => UpdateSelectionStatus();
        _grid.ColumnHeaderMouseClick += (_, _) => SetStatus("Click a column header to sort.");
        _grid.KeyDown += GridKeyDown;

        dataPanel.Controls.Add(_grid);
        dataPanel.Controls.Add(searchPanel);
        dataPage.Controls.Add(dataPanel);

        var structurePage = new TabPage("Structure");
        ConfigureGrid(_structureGrid);
        structurePage.Controls.Add(_structureGrid);

        _resultTabs.TabPages.Add(dataPage);
        _resultTabs.TabPages.Add(structurePage);
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = true;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = Color.White;
        grid.RowHeadersVisible = false;
        grid.AllowUserToResizeColumns = true;
    }

    private static void AddButton(ToolStrip bar, string text, EventHandler handler)
    {
        var button = new ToolStripButton(text);
        button.Click += handler;
        bar.Items.Add(button);
    }

    private void NewDatabase()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "SQLite (*.db;*.sqlite;*.sqlite3)|*.db;*.sqlite;*.sqlite3|All files|*.*",
            DefaultExt = "db",
            FileName = "database.db",
            Title = "Create SQLite Database"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            CloseConnection();
            _path = dialog.FileName;
            OpenConnection();
            LoadSchema();
            SetStatus("New SQLite database created/opened.");
        }
        catch (Exception ex) { ShowError("Could not create database.", ex); }
    }

    private void OpenDatabase()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "SQLite (*.db;*.sqlite;*.sqlite3)|*.db;*.sqlite;*.sqlite3|All files|*.*",
            Title = "Open SQLite Database"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            CloseConnection();
            _path = dialog.FileName;
            OpenConnection();
            LoadSchema();
            SetStatus("SQLite database opened.");
        }
        catch (Exception ex) { ShowError("Could not open database.", ex); }
    }

    private void OpenConnection()
    {
        if (_path is null) return;
        _db = new SqliteConnection($"Data Source={_path}");
        _db.Open();
        _dbLabel.Text = $"Database: {_path}";
    }

    private void CloseDatabase()
    {
        CloseConnection();
        _tree.Nodes.Clear();
        _source.DataSource = null;
        _structureGrid.DataSource = null;
        _currentData = null;
        _currentTable = null;
        _path = null;
        _dbLabel.Text = "No database open";
        _rowStatus.Text = "";
        SetStatus("Database closed.");
    }

    private void CloseConnection()
    {
        _db?.Dispose();
        _db = null;
    }

    private void LoadSchema()
    {
        if (_db is null)
        {
            SetStatus("No database is open.");
            return;
        }

        try
        {
            _tree.Nodes.Clear();
            var groups = new Dictionary<string, TreeNode>
            {
                ["table"] = new TreeNode("Tables") { Tag = new SchemaNode("group", "table") },
                ["view"] = new TreeNode("Views") { Tag = new SchemaNode("group", "view") },
                ["index"] = new TreeNode("Indexes") { Tag = new SchemaNode("group", "index") },
                ["trigger"] = new TreeNode("Triggers") { Tag = new SchemaNode("group", "trigger") }
            };

            using var command = _db.CreateCommand();
            command.CommandText = """
                SELECT name, type
                FROM sqlite_master
                WHERE type IN ('table','view','index','trigger')
                  AND name NOT LIKE 'sqlite_%'
                ORDER BY type, name;
                """;

            using var reader = command.ExecuteReader();
            var counts = new Dictionary<string, int>();
            while (reader.Read())
            {
                var name = reader.GetString(0);
                var type = reader.GetString(1);
                groups[type].Nodes.Add(new TreeNode(name) { Tag = new SchemaNode(type, name) });
                counts[type] = counts.GetValueOrDefault(type) + 1;
            }

            foreach (var group in groups.Values)
            {
                if (group.Nodes.Count == 0) continue;
                _tree.Nodes.Add(group);
                group.Expand();
            }

            SetStatus(
                $"Schema refreshed: {counts.GetValueOrDefault("table")} table(s), " +
                $"{counts.GetValueOrDefault("view")} view(s), " +
                $"{counts.GetValueOrDefault("index")} index(es), " +
                $"{counts.GetValueOrDefault("trigger")} trigger(s).");
        }
        catch (Exception ex) { ShowError("Could not load schema.", ex); }
    }

    private void SelectObject(SchemaNode node)
    {
        _currentTable = node.Name;
        LoadTableData(node.Name);
        LoadStructure(node.Name);
        _resultTabs.SelectedIndex = 0;
    }

    private void LoadTableData(string tableName)
    {
        try
        {
            using var command = _db!.CreateCommand();
            command.CommandText = $"SELECT rowid AS __jass_rowid, * FROM \"{Quote(tableName)}\";";
            using var reader = command.ExecuteReader();

            var table = new DataTable();
            table.Load(reader);
            _currentData = table;
            _source.DataSource = table;
            HideRowIdColumn();
            _search.Clear();
            _rowStatus.Text = $"{table.Rows.Count:N0} row(s) | {table.Columns.Count - 1:N0} data column(s)";
            SetStatus($"Loaded table: {tableName}");
        }
        catch (Exception ex) { ShowError($"Could not load table '{tableName}'.", ex); }
    }

    private void HideRowIdColumn()
    {
        if (_grid.Columns.Contains("__jass_rowid"))
            _grid.Columns["__jass_rowid"].Visible = false;
    }

    private void LoadStructure(string tableName)
    {
        try
        {
            using var command = _db!.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{Quote(tableName)}\");";
            using var reader = command.ExecuteReader();

            var dt = new DataTable();
            dt.Columns.Add("Column");
            dt.Columns.Add("Type");
            dt.Columns.Add("Not Null");
            dt.Columns.Add("Default");
            dt.Columns.Add("Primary Key");

            while (reader.Read())
            {
                dt.Rows.Add(
                    reader["name"],
                    reader["type"],
                    Convert.ToInt32(reader["notnull"]) == 1 ? "YES" : "NO",
                    reader["dflt_value"] == DBNull.Value ? "" : reader["dflt_value"],
                    Convert.ToInt32(reader["pk"]) == 1 ? "YES" : "NO");
            }

            _structureGrid.DataSource = dt;
        }
        catch (Exception ex) { ShowError("Could not read table structure.", ex); }
    }

    private static string Quote(string name) => name.Replace("\"", "\"\"");

    private void ApplySearch()
    {
        if (_currentData is null) return;

        var text = _search.Text.Trim().Replace("'", "''");
        try
        {
            if (string.IsNullOrEmpty(text))
            {
                _source.RemoveFilter();
            }
            else
            {
                var expressions = _currentData.Columns.Cast<DataColumn>()
                    .Where(c => c.ColumnName != "__jass_rowid")
                    .Select(c =>
                        $"CONVERT([{c.ColumnName.Replace("]", "]]")}], 'System.String') LIKE '%{text}%'");
                _source.Filter = string.Join(" OR ", expressions);
            }

            _rowStatus.Text =
                $"{_source.Count:N0} visible row(s) | {_currentData.Columns.Count - 1:N0} data column(s)";
        }
        catch
        {
            _source.RemoveFilter();
            SetStatus("Search could not be applied.");
        }
    }

    private void UpdateSelectionStatus()
    {
        if (_grid.SelectedRows.Count > 0)
            SetStatus($"{_grid.SelectedRows.Count:N0} row(s) selected.");
    }

    private void GridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.C)
        {
            CopySelectedCells();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Delete)
        {
            DeleteSelectedRows();
            e.Handled = true;
        }
    }

    private void CopySelectedCells()
    {
        if (_grid.SelectedCells.Count == 0)
        {
            SetStatus("No cells selected.");
            return;
        }

        var rows = _grid.SelectedCells.Cast<DataGridViewCell>()
            .GroupBy(c => c.RowIndex)
            .OrderBy(g => g.Key);

        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join("\t",
                row.OrderBy(c => c.ColumnIndex).Select(c => c.Value?.ToString() ?? "")));
        }

        Clipboard.SetText(sb.ToString().TrimEnd());
        SetStatus("Selected cells copied to clipboard.");
    }

    private void AddRow()
    {
        if (_db is null || string.IsNullOrWhiteSpace(_currentTable))
        {
            SetStatus("Select a table first.");
            return;
        }

        using var dialog = new RowEditorForm(_currentData, null);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            InsertRow(dialog.Values);
            ReloadCurrentTable();
            SetStatus("Row inserted successfully.");
        }
        catch (Exception ex) { ShowError("Could not insert row.", ex); }
    }

    private void EditSelectedRow()
    {
        if (_db is null || string.IsNullOrWhiteSpace(_currentTable))
        {
            SetStatus("Select a table first.");
            return;
        }

        if (_grid.SelectedRows.Count != 1)
        {
            MessageBox.Show(this, "Select exactly one row to edit.", "Edit Row",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var row = _grid.SelectedRows[0];
        if (row.DataBoundItem is not DataRowView view)
        {
            SetStatus("The selected row cannot be edited.");
            return;
        }

        using var dialog = new RowEditorForm(_currentData, view.Row);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            UpdateRow(view.Row, dialog.Values);
            ReloadCurrentTable();
            SetStatus("Row updated successfully.");
        }
        catch (Exception ex) { ShowError("Could not update row.", ex); }
    }

    private void DeleteSelectedRows()
    {
        if (_db is null || string.IsNullOrWhiteSpace(_currentTable))
        {
            SetStatus("Select a table first.");
            return;
        }

        if (_grid.SelectedRows.Count == 0)
        {
            SetStatus("Select one or more rows first.");
            return;
        }

        if (MessageBox.Show(
            this,
            $"Delete {_grid.SelectedRows.Count:N0} selected row(s)?\n\nThis action cannot be undone.",
            "Delete Rows",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            var ids = _grid.SelectedRows.Cast<DataGridViewRow>()
                .Where(r => r.DataBoundItem is DataRowView)
                .Select(r => Convert.ToInt64(((DataRowView)r.DataBoundItem)["__jass_rowid"]))
                .ToList();

            using var transaction = _db.BeginTransaction();
            using var command = _db.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"DELETE FROM \"{Quote(_currentTable)}\" WHERE rowid = $id;";
            var parameter = command.Parameters.Add("$id", SqliteType.Integer);

            foreach (var id in ids)
            {
                parameter.Value = id;
                command.ExecuteNonQuery();
            }

            transaction.Commit();
            ReloadCurrentTable();
            SetStatus($"{ids.Count:N0} row(s) deleted.");
        }
        catch (Exception ex) { ShowError("Could not delete rows.", ex); }
    }

    private void InsertRow(Dictionary<string, string?> values)
    {
        if (values.Count == 0) return;

        var columns = values.Keys.Select(Quote).ToArray();
        var parameters = values.Keys.Select((_, i) => "$p" + i).ToArray();

        using var command = _db!.CreateCommand();
        command.CommandText =
            $"INSERT INTO \"{Quote(_currentTable!)}\" ({string.Join(",", columns)}) VALUES ({string.Join(",", parameters)});";

        var i = 0;
        foreach (var value in values.Values)
            command.Parameters.AddWithValue("$p" + i++, value is null ? DBNull.Value : value);

        command.ExecuteNonQuery();
    }

    private void UpdateRow(DataRow row, Dictionary<string, string?> values)
    {
        var rowid = Convert.ToInt64(row["__jass_rowid"]);
        var sets = values.Keys.Select((key, i) => $"\"{Quote(key)}\"=$p{i}");

        using var command = _db!.CreateCommand();
        command.CommandText =
            $"UPDATE \"{Quote(_currentTable!)}\" SET {string.Join(",", sets)} WHERE rowid=$rowid;";
        command.Parameters.AddWithValue("$rowid", rowid);

        var i = 0;
        foreach (var value in values.Values)
            command.Parameters.AddWithValue("$p" + i++, value is null ? DBNull.Value : value);

        command.ExecuteNonQuery();
    }

    private void ReloadCurrentTable()
    {
        if (!string.IsNullOrWhiteSpace(_currentTable))
            LoadTableData(_currentTable);
    }

    private void ExecuteSql()
    {
        if (_db is null)
        {
            SetStatus("Open a database first.");
            return;
        }

        var query = _sql.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            SetStatus("SQL editor is empty.");
            return;
        }

        try
        {
            if (!_history.Contains(query))
                _history.Insert(0, query);
            if (_history.Count > 50)
                _history.RemoveAt(50);

            using var command = _db.CreateCommand();
            command.CommandText = query;

            if (IsQuery(query))
            {
                using var reader = command.ExecuteReader();
                var table = new DataTable();
                table.Load(reader);
                _currentData = table;
                _source.DataSource = table;
                _search.Clear();
                _rowStatus.Text = $"{table.Rows.Count:N0} row(s) returned | {table.Columns.Count:N0} column(s)";
                SetStatus("SQL query executed successfully.");
            }
            else
            {
                var affected = command.ExecuteNonQuery();
                _rowStatus.Text = $"{affected:N0} row(s) affected";
                LoadSchema();
                SetStatus("SQL command executed successfully.");
            }
        }
        catch (Exception ex) { ShowError("SQL execution failed.", ex); }
    }

    private static bool IsQuery(string query)
    {
        var s = query.TrimStart().ToUpperInvariant();
        return s.StartsWith("SELECT ") || s.StartsWith("WITH ") ||
               s.StartsWith("PRAGMA ") || s.StartsWith("EXPLAIN ") ||
               s.StartsWith("VALUES ");
    }

    private void ExportCurrent()
    {
        if (_grid.Rows.Count == 0 || _grid.Columns.Count == 0)
        {
            SetStatus("No grid data to export.");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv|JSON (*.json)|*.json",
            FileName = (_currentTable ?? "query_result") + ".csv"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            if (Path.GetExtension(dialog.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase))
                ExportJson(dialog.FileName);
            else
                ExportCsv(dialog.FileName);

            SetStatus($"Exported: {dialog.FileName}");
        }
        catch (Exception ex) { ShowError("Export failed.", ex); }
    }

    private void ExportCsv(string path)
    {
        var sb = new StringBuilder();
        var columns = _grid.Columns.Cast<DataGridViewColumn>()
            .Where(c => c.Visible)
            .ToList();

        sb.AppendLine(string.Join(",", columns.Select(c => Csv(c.HeaderText))));
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            sb.AppendLine(string.Join(",", columns.Select(c => Csv(row.Cells[c.Index].Value?.ToString() ?? ""))));
        }

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }

    private void ExportJson(string path)
    {
        var columns = _grid.Columns.Cast<DataGridViewColumn>()
            .Where(c => c.Visible)
            .ToList();

        var list = new List<Dictionary<string, object?>>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            var item = new Dictionary<string, object?>();
            foreach (var column in columns)
                item[column.HeaderText] = row.Cells[column.Index].Value;
            list.Add(item);
        }

        File.WriteAllText(path,
            JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }),
            Encoding.UTF8);
    }

    private static string Csv(string value) =>
        value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;

    private void ShowHistory()
    {
        using var form = new Form
        {
            Text = "SQL History",
            Width = 850,
            Height = 500,
            StartPosition = FormStartPosition.CenterParent
        };

        var list = new ListBox { Dock = DockStyle.Fill };
        list.Items.AddRange(_history.ToArray());

        var panel = new Panel { Dock = DockStyle.Bottom, Height = 42 };
        var use = new Button { Text = "Use Selected", Dock = DockStyle.Right, Width = 110 };
        use.Click += (_, _) =>
        {
            if (list.SelectedItem is string query)
            {
                _sql.Text = query;
                form.Close();
            }
        };
        panel.Controls.Add(use);

        form.Controls.Add(list);
        form.Controls.Add(panel);
        form.ShowDialog(this);
    }

    private void ShowDatabaseInfo()
    {
        if (_db is null)
        {
            SetStatus("No database is open.");
            return;
        }

        try
        {
            using var command = _db.CreateCommand();
            command.CommandText = """
                SELECT
                  (SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'),
                  (SELECT COUNT(*) FROM sqlite_master WHERE type='view' AND name NOT LIKE 'sqlite_%'),
                  (SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name NOT LIKE 'sqlite_%'),
                  (SELECT COUNT(*) FROM sqlite_master WHERE type='trigger' AND name NOT LIKE 'sqlite_%'),
                  sqlite_version();
                """;

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                MessageBox.Show(
                    this,
                    $"SQLite version: {reader.GetString(4)}\n" +
                    $"Tables: {reader.GetInt64(0):N0}\n" +
                    $"Views: {reader.GetInt64(1):N0}\n" +
                    $"Indexes: {reader.GetInt64(2):N0}\n" +
                    $"Triggers: {reader.GetInt64(3):N0}\n\nFile:\n{_path}",
                    "Database Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex) { ShowError("Could not read database information.", ex); }
    }

    private void RunMaintenance(string commandText)
    {
        if (_db is null)
        {
            SetStatus("No database is open.");
            return;
        }

        try
        {
            using var command = _db.CreateCommand();
            command.CommandText = commandText;
            command.ExecuteNonQuery();
            SetStatus($"{commandText} completed successfully.");
        }
        catch (Exception ex) { ShowError($"{commandText} failed.", ex); }
    }

    private void SetStatus(string message) => _status.Text = "  " + message;

    private void ShowError(string message, Exception ex)
    {
        SetStatus(message);
        MessageBox.Show(this, $"{message}\n\n{ex.Message}",
            "JASS SQLite Explorer", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        CloseConnection();
        base.OnFormClosed(e);
    }

    private sealed record SchemaNode(string Type, string Name);
}

internal sealed class RowEditorForm : Form
{
    private readonly TableLayoutPanel _layout = new();
    private readonly Dictionary<string, TextBox> _boxes = new();
    public Dictionary<string, string?> Values { get; } = new();

    public RowEditorForm(DataTable? table, DataRow? row)
    {
        Text = row is null ? "Add Row" : "Edit Row";
        Width = 620;
        Height = 620;
        MinimumSize = new Size(500, 400);
        StartPosition = FormStartPosition.CenterParent;

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(12) };
        _layout.Dock = DockStyle.Top;
        _layout.AutoSize = true;
        _layout.ColumnCount = 2;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        if (table is not null)
        {
            foreach (DataColumn column in table.Columns)
            {
                if (column.ColumnName == "__jass_rowid") continue;

                var label = new Label
                {
                    Text = column.ColumnName,
                    AutoSize = true,
                    Margin = new Padding(3, 8, 3, 3)
                };

                var box = new TextBox { Width = 380 };
                if (row is not null)
                    box.Text = row[column] == DBNull.Value ? "" : row[column]?.ToString() ?? "";

                _boxes[column.ColumnName] = box;
                _layout.Controls.Add(label);
                _layout.Controls.Add(box);
            }
        }

        scroll.Controls.Add(_layout);

        var buttons = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(8) };
        var ok = new Button { Text = "Save", DialogResult = DialogResult.OK, Dock = DockStyle.Right, Width = 90 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Dock = DockStyle.Right, Width = 90 };
        ok.Click += (_, _) =>
        {
            Values.Clear();
            foreach (var pair in _boxes)
                Values[pair.Key] = string.IsNullOrEmpty(pair.Value.Text) ? null : pair.Value.Text;
        };

        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        AcceptButton = ok;
        CancelButton = cancel;

        Controls.Add(scroll);
        Controls.Add(buttons);
    }
}
