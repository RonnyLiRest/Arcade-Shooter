using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using ArcadeShooter.Managers;

namespace ArcadeShooter.Forms
{
    /// <summary>
    /// Главная форма приложения. Отвечает только за представление
    /// (отрисовку), ввод пользователя и элементы управления (кнопки);
    /// вся игровая логика находится в GameManager.
    /// </summary>
    public partial class MainForm : Form
    {
        private const int FieldWidth = 780;
        private const int FieldHeight = 820;

        private static readonly Color PanelBg = Color.FromArgb(90, 0, 0, 0);
        private static readonly Color PanelBorder = Color.FromArgb(180, 255, 255, 255);
        private static readonly Color AccentCyan = Color.FromArgb(0, 210, 210);
        private static readonly Color AccentGold = Color.Gold;
        private static readonly Color AccentRed = Color.FromArgb(220, 70, 70);

        private readonly GameManager _gameManager;
        private readonly System.Windows.Forms.Timer _gameTimer;
        private readonly HashSet<Keys> _pressedKeys = new();
        private DateTime _lastTick;

        // Кнопки меню
        private Button _btnStart = null!;
        private Button _btnHighScores = null!;
        private Button _btnExit = null!;

        // Кнопки паузы
        private Button _btnResume = null!;
        private Button _btnPauseToMenu = null!;

        // Кнопка экрана рекордов
        private Button _btnScoresBack = null!;

        // Кнопки экрана окончания игры
        private Button _btnNewGame = null!;
        private Button _btnGameOverMenu = null!;
        private Button _btnGameOverExit = null!;

        // Клавиши, которые обрабатывает сама игра (а не фокус WinForms-контролов).
        // Button в WinForms перехватывает Space/Enter как "нажать эту кнопку", даже
        // если он скрыт, но всё ещё удерживает фокус — поэтому для этих клавиш
        // явно помечаем событие как обработанное на уровне формы (см. MainForm_KeyDown).
        private static readonly HashSet<Keys> GameKeys = new()
        {
            Keys.Left, Keys.Right, Keys.Up, Keys.Down,
            Keys.A, Keys.S, Keys.D, Keys.W,
            Keys.Space, Keys.Escape, Keys.Enter
        };

        // Тряска экрана при получении урона: время, оставшееся до конца эффекта, и его сила.
        private const float ShakeDuration = 0.25f;
        private const float ShakeMaxOffset = 6f;
        private float _shakeTimer;
        private readonly Random _shakeRandom = new();

        public MainForm()
        {
            InitializeComponent();

            ClientSize = new Size(FieldWidth, FieldHeight);
            Text = "Arcade Shooter"; 
            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = Color.Black;

            _gameManager = new GameManager(new Rectangle(0, 0, FieldWidth, FieldHeight));
            _gameManager.ScoreChanged += _ => Invalidate();
            _gameManager.GameOver += () => { SyncButtonsToState(); Invalidate(); };
            _gameManager.PlayerDamaged += () => _shakeTimer = ShakeDuration;

            BuildButtons();
            SyncButtonsToState();

            _gameTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _gameTimer.Tick += GameTimer_Tick;
            _lastTick = DateTime.Now;
            _gameTimer.Start();

            KeyDown += MainForm_KeyDown;
            KeyUp += MainForm_KeyUp;
            Paint += MainForm_Paint;
        }

        // ---------------------------------------------------------------
        //  Создание и стилизация кнопок
        // ---------------------------------------------------------------

        private void BuildButtons()
        {
            int centerX = FieldWidth / 2;

            _btnStart = CreateStyledButton("Начать игру", Color.MediumPurple);
            _btnHighScores = CreateStyledButton("Таблица рекордов", AccentGold);
            _btnExit = CreateStyledButton("Выход", AccentRed);
            LayoutStack(centerX, 430, _btnStart, _btnHighScores, _btnExit);

            _btnResume = CreateStyledButton("Продолжить игру", AccentCyan);
            _btnPauseToMenu = CreateStyledButton("Выйти в главное меню", AccentRed);
            LayoutStack(centerX, 470, _btnResume, _btnPauseToMenu);

            _btnScoresBack = CreateStyledButton("Назад", AccentGold);
            _btnScoresBack.Location = new Point(centerX - _btnScoresBack.Width / 2, FieldHeight - 250);

            _btnNewGame = CreateStyledButton("Новая игра", Color.MediumPurple);
            _btnGameOverMenu = CreateStyledButton("Главное меню", AccentGold);
            _btnGameOverExit = CreateStyledButton("Выход", AccentRed);
            LayoutStack(centerX, 520, _btnNewGame, _btnGameOverMenu, _btnGameOverExit);

            _btnStart.Click += (_, _) => { _gameManager.StartNewGame(); SyncButtonsToState(); };
            _btnHighScores.Click += (_, _) => { _gameManager.ShowHighScoresScreen(); SyncButtonsToState(); };
            _btnExit.Click += (_, _) => Close();
            _btnResume.Click += (_, _) => { _gameManager.ResumeGame(); SyncButtonsToState(); };
            _btnPauseToMenu.Click += (_, _) => { _gameManager.ReturnToMenu(); SyncButtonsToState(); };
            _btnScoresBack.Click += (_, _) => { _gameManager.ReturnToMenu(); SyncButtonsToState(); };
            _btnNewGame.Click += (_, _) => { _gameManager.StartNewGame(); SyncButtonsToState(); };
            _btnGameOverMenu.Click += (_, _) => { _gameManager.ReturnToMenu(); SyncButtonsToState(); };
            _btnGameOverExit.Click += (_, _) => Close();

            Controls.AddRange(new Control[]
            {
                _btnStart, _btnHighScores, _btnExit,
                _btnResume, _btnPauseToMenu,
                _btnScoresBack,
                _btnNewGame, _btnGameOverMenu, _btnGameOverExit
            });
        }

        /// <summary>Ставит кнопки в вертикальный стек по центру, друг под другом.</summary>
        private void LayoutStack(int centerX, int topY, params Button[] buttons)
        {
            int y = topY;
            foreach (var btn in buttons)
            {
                btn.Location = new Point(centerX - btn.Width / 2, y);
                y += btn.Height + 16;
            }
        }

        private Button CreateStyledButton(string text, Color accent)
        {
            var btn = new Button
            {
                Text = text,
                Width = 280,
                Height = 48,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(18, 18, 24),
                ForeColor = Color.White,
                Font = new Font("Consolas", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                TabStop = false,
            };
            btn.FlatAppearance.BorderSize = 2;
            btn.FlatAppearance.BorderColor = accent;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(Color.FromArgb(18, 18, 24), 0.15f);
            btn.FlatAppearance.MouseDownBackColor = accent;

            // Лёгкая подсветка текста при наведении — чтобы кнопка ощущалась "живой".
            btn.MouseEnter += (_, _) => btn.ForeColor = accent;
            btn.MouseLeave += (_, _) => btn.ForeColor = Color.White;

            return btn;
        }

        /// <summary>Показывает только те кнопки, которые относятся к текущему состоянию игры.</summary>
        private void SyncButtonsToState()
        {
            bool isMenu = _gameManager.State == GameState.Menu;
            bool isPaused = _gameManager.State == GameState.Paused;
            bool isHighScores = _gameManager.State == GameState.HighScores;
            bool isGameOver = _gameManager.State == GameState.GameOver;

            _btnStart.Visible = isMenu;
            _btnHighScores.Visible = isMenu;
            _btnExit.Visible = isMenu;

            _btnResume.Visible = isPaused;
            _btnPauseToMenu.Visible = isPaused;

            _btnScoresBack.Visible = isHighScores;

            _btnNewGame.Visible = isGameOver;
            _btnGameOverMenu.Visible = isGameOver;
            _btnGameOverExit.Visible = isGameOver;

            // Снимаем фокус с любой кнопки: иначе скрытая, но всё ещё
            // сфокусированная кнопка может перехватить Space/Enter как собственный клик.
            ActiveControl = null;
        }

        // ---------------------------------------------------------------
        //  Игровой цикл и ввод
        // ---------------------------------------------------------------

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            float deltaTime = (float)(now - _lastTick).TotalSeconds;
            _lastTick = now;
            deltaTime = Math.Min(deltaTime, 0.05f);

            if (_gameManager.State == GameState.Playing)
            {
                HandleContinuousInput(deltaTime);
                _gameManager.Update(deltaTime);
            }

            if (_shakeTimer > 0f) _shakeTimer -= deltaTime;

            Invalidate();
        }

        private void HandleContinuousInput(float deltaTime)
        {
            float dx = 0f, dy = 0f;
            if (_pressedKeys.Contains(Keys.Left) || _pressedKeys.Contains(Keys.A)) dx -= 1f;
            if (_pressedKeys.Contains(Keys.Right) || _pressedKeys.Contains(Keys.D)) dx += 1f;
            if (_pressedKeys.Contains(Keys.Up) || _pressedKeys.Contains(Keys.W)) dy -= 1f;
            if (_pressedKeys.Contains(Keys.Down) || _pressedKeys.Contains(Keys.S)) dy += 1f;

            if (dx != 0f || dy != 0f)
            {
                float length = (float)Math.Sqrt(dx * dx + dy * dy);
                dx /= length;
                dy /= length;
                _gameManager.Player.MoveBy(dx, dy, deltaTime, _gameManager.FieldBounds);
            }

            if (_pressedKeys.Contains(Keys.Space))
            {
                _gameManager.HandleShootInput();
            }
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            _pressedKeys.Add(e.KeyCode);

            // Клавиши дублируют кнопки (быстрый доступ), сами кнопки остаются основным способом навигации.
            switch (_gameManager.State)
            {
                case GameState.Menu:
                    if (e.KeyCode == Keys.Enter) _gameManager.StartNewGame();
                    else if (e.KeyCode == Keys.Escape) Close();
                    break;

                case GameState.Playing:
                    if (e.KeyCode == Keys.Escape) _gameManager.PauseGame();
                    break;

                case GameState.Paused:
                    if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter) _gameManager.ResumeGame();
                    break;

                case GameState.HighScores:
                    if (e.KeyCode == Keys.Escape) _gameManager.ReturnToMenu();
                    break;
            }

            // Гасим дальнейшую обработку этих клавиш контролами формы (кнопками):
            // без этого Space/Enter могли долетать до случайно сфокусированной
            // кнопки и "нажимать" её повторно поверх нашей собственной логики.
            if (GameKeys.Contains(e.KeyCode))
            {
                e.Handled = true;
            }

            SyncButtonsToState();
            Invalidate();
        }

        private void MainForm_KeyUp(object? sender, KeyEventArgs e)
        {
            _pressedKeys.Remove(e.KeyCode);
        }

        // ---------------------------------------------------------------
        //  Отрисовка
        // ---------------------------------------------------------------

        private void MainForm_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            bool isGameplayState = _gameManager.State == GameState.Playing || _gameManager.State == GameState.Paused;

            if (isGameplayState)
            {
                // у каждого уровня своя картинка фона
                g.DrawImage(SpriteManager.Get($"level{_gameManager.CurrentLevel.Number}_bg.png"), ClientRectangle);
            }
            else
            {
                // меню, таблица рекордов и экран Game Over — сплошной тёмный фон
                using var bg = new SolidBrush(Color.FromArgb(8, 8, 14));
                g.FillRectangle(bg, ClientRectangle);
            }

            switch (_gameManager.State)
            {
                case GameState.Menu:
                    DrawMenuScreen(g);
                    break;

                case GameState.Playing:
                    ApplyShake(g);
                    _gameManager.DrawAll(g);
                    g.ResetTransform(); // HUD не трясётся, чтобы текст оставался читаемым
                    DrawHud(g);
                    break;

                case GameState.Paused:
                    _gameManager.DrawAll(g);
                    DrawHud(g);
                    DrawPauseOverlay(g);
                    break;

                case GameState.GameOver:
                    DrawGameOverScreen(g);
                    break;

                case GameState.HighScores:
                    DrawHighScoresScreen(g);
                    break;
            }
        }

        /// <summary>Смещает систему координат на случайный вектор; амплитуда затухает вместе с таймером.</summary>
        private void ApplyShake(Graphics g)
        {
            if (_shakeTimer <= 0f) return;

            float strength = ShakeMaxOffset * (_shakeTimer / ShakeDuration);
            float dx = ((float)_shakeRandom.NextDouble() * 2f - 1f) * strength;
            float dy = ((float)_shakeRandom.NextDouble() * 2f - 1f) * strength;
            g.TranslateTransform(dx, dy);
        }

        private void DrawMenuScreen(Graphics g)
        {
            using var titleFont = new Font("Consolas", 34, FontStyle.Bold);
            using var subFont = new Font("Consolas", 12, FontStyle.Regular);
            using var titleBrush = new SolidBrush(Color.White);
            using var subBrush = new SolidBrush(Color.LightGray);

            string title = "ARCADE SHOOTER";
            var titleSize = g.MeasureString(title, titleFont);
            g.DrawString(title, titleFont, titleBrush, (ClientSize.Width - titleSize.Width) / 2, 140);

            string sub = "WASD / стрелки — движение     Space — огонь     Esc — пауза";
            var subSize = g.MeasureString(sub, subFont);
            g.DrawString(sub, subFont, subBrush, (ClientSize.Width - subSize.Width) / 2, 210);

            // декоративная линия-разделитель под заголовком
            using var linePen = new Pen(Color.White, 2f);
            float lineY = 260;
            g.DrawLine(linePen, ClientSize.Width / 2f - 190, lineY, ClientSize.Width / 2f + 190, lineY);
        }

        private void DrawHud(Graphics g)
        {
            using var font = new Font("Consolas", 12, FontStyle.Bold);
            using var brush = new SolidBrush(Color.White);
            g.DrawString($"Счёт: {_gameManager.Player.Score}", font, brush, 10, 8);
            g.DrawString($"Жизни: {_gameManager.Player.Lives}", font, brush, 10, 28);
            g.DrawString($"Уровень {_gameManager.CurrentLevel.Number}: {_gameManager.CurrentLevel.Name}", font, brush, 10, 48);

            DrawPowerUpLegend(g, top: 74);
            DrawRapidFireTimer(g, left: 8, top: 168);
            DrawPauseHint(g);
        }

        /// <summary>Рамка в левом верхнем углу под уровнем — подсказка по бонусам.</summary>
        private void DrawPowerUpLegend(Graphics g, float top)
        {
            using var font = new Font("Consolas", 9, FontStyle.Regular);
            using var textBrush = new SolidBrush(Color.White);
            using var borderPen = new Pen(PanelBorder, 1f);
            using var bgBrush = new SolidBrush(PanelBg);

            const float boxX = 8f;
            const float boxWidth = 180f;
            const float boxHeight = 86f;

            g.FillRectangle(bgBrush, boxX, top, boxWidth, boxHeight);
            g.DrawRectangle(borderPen, boxX, top, boxWidth, boxHeight);

            DrawLegendRow(g, font, textBrush, "health.png", "Лечение", boxX + 8, top + 8);
            DrawLegendRow(g, font, textBrush, "rapidfire.png", "Усиление огня", boxX + 8, top + 34);
            DrawLegendRow(g, font, textBrush, "extralife.png", "Доп. жизнь", boxX + 8, top + 60);
        }

        private void DrawLegendRow(Graphics g, Font font, Brush textBrush, string spriteName, string label, float x, float y)
        {
            g.DrawImage(SpriteManager.Get(spriteName), x, y + 2, 16, 16);
            g.DrawString(label, font, textBrush, x + 22, y);
        }

        /// <summary>
        /// Круглый таймер оставшегося времени баффа RapidFire (двойной выстрел + ускорение).
        /// Рисуется только пока бафф активен, сразу под рамкой-легендой бонусов.
        /// </summary>
        private void DrawRapidFireTimer(Graphics g, float left, float top)
        {
            var player = _gameManager.Player;
            if (!player.IsRapidFireActive) return;

            const float diameter = 52f;
            float ratio = player.RapidFireDurationSeconds > 0f
                ? Math.Clamp(player.RapidFireRemainingSeconds / player.RapidFireDurationSeconds, 0f, 1f)
                : 0f;
            float sweepAngle = 360f * ratio;

            using var bgBrush = new SolidBrush(PanelBg);
            using var borderPen = new Pen(PanelBorder, 1.5f);
            using var arcBrush = new SolidBrush(Color.FromArgb(220, AccentCyan));

            g.FillEllipse(bgBrush, left, top, diameter, diameter);
            // -90 градусов — старт с 12 часов, дальше по часовой стрелке "стаивает" по мере убывания времени.
            g.FillPie(arcBrush, left, top, diameter, diameter, -90f, sweepAngle);
            g.DrawEllipse(borderPen, left, top, diameter, diameter);
        }

        /// <summary>Подсказка "Пауза - Esc" в рамке, прижатая к правому верхнему углу.</summary>
        private void DrawPauseHint(Graphics g)
        {
            using var font = new Font("Consolas", 11, FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.White);
            using var borderPen = new Pen(PanelBorder, 1f);
            using var bgBrush = new SolidBrush(PanelBg);

            const string text = "Пауза - Esc";
            var size = g.MeasureString(text, font);
            const float paddingX = 10f, paddingY = 6f;
            float boxWidth = size.Width + paddingX * 2;
            float boxHeight = size.Height + paddingY * 2;
            float boxX = ClientSize.Width - boxWidth - 12;
            const float boxY = 8f;

            g.FillRectangle(bgBrush, boxX, boxY, boxWidth, boxHeight);
            g.DrawRectangle(borderPen, boxX, boxY, boxWidth, boxHeight);
            g.DrawString(text, font, textBrush, boxX + paddingX, boxY + paddingY);
        }

        private void DrawPauseOverlay(Graphics g)
        {
            using (var dim = new SolidBrush(Color.FromArgb(175, 0, 0, 0)))
            {
                g.FillRectangle(dim, ClientRectangle);
            }

            using var titleFont = new Font("Consolas", 28, FontStyle.Bold);
            using var warnFont = new Font("Consolas", 11, FontStyle.Bold);
            using var titleBrush = new SolidBrush(Color.White);
            using var warnBrush = new SolidBrush(AccentRed);

            string title = "ПАУЗА";
            var titleSize = g.MeasureString(title, titleFont);
            g.DrawString(title, titleFont, titleBrush, (ClientSize.Width - titleSize.Width) / 2, 300);

            string warning = "Внимание: при выходе в главное меню\nрезультат текущей партии не сохранится!";
            using var centered = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString(warning, warnFont, warnBrush, new RectangleF(0, 370, ClientSize.Width, 60), centered);
        }

        private void DrawGameOverScreen(Graphics g)
        {
            using var titleFont = new Font("Consolas", 32, FontStyle.Bold);
            using var headerFont = new Font("Consolas", 14, FontStyle.Bold);
            using var whiteBrush = new SolidBrush(Color.White);
            using var goldBrush = new SolidBrush(AccentGold);

            string title = "ИГРА ОКОНЧЕНА";
            var titleSize = g.MeasureString(title, titleFont);
            float titleY = 60;
            g.DrawString(title, titleFont, whiteBrush, (ClientSize.Width - titleSize.Width) / 2, titleY);

            string scoreLine = $"Ваш счёт: {_gameManager.Player.Score}";
            var scoreSize = g.MeasureString(scoreLine, headerFont);
            g.DrawString(scoreLine, headerFont, goldBrush, (ClientSize.Width - scoreSize.Width) / 2, titleY + 90);

            DrawHighScoresList(g, top: titleY + 150, highlightScore: _gameManager.Player.Score);
        }

        private void DrawHighScoresScreen(Graphics g)
        {
            using var headerFont = new Font("Consolas", 22, FontStyle.Bold);
            using var whiteBrush = new SolidBrush(Color.White);

            string header = "ТОП-10 РЕКОРДОВ";
            var headerSize = g.MeasureString(header, headerFont);
            float top = 70;
            g.DrawString(header, headerFont, whiteBrush, (ClientSize.Width - headerSize.Width) / 2, top);

            DrawHighScoresList(g, top: top + 120, highlightScore: null);
        }

        /// <summary>Общий рендер таблицы рекордов — используется и на Game Over, и в отдельном экране из меню.</summary>
        private void DrawHighScoresList(Graphics g, float top, int? highlightScore)
        {
            using var rowFont = new Font("Consolas", 13, FontStyle.Regular);
            using var goldBrush = new SolidBrush(AccentGold);
            using var grayBrush = new SolidBrush(Color.LightGray);

            var scores = _gameManager.LastHighScores;
            const float rowHeight = 26;
            float centerX = ClientSize.Width / 2f;
            bool alreadyHighlighted = false;

            // две колонки: номера по левому краю, результаты по правому (разряды стоят друг под другом)
            const float tableWidth = 260f;
            const float numberColumnWidth = 60f;
            float left = centerX - tableWidth / 2f;
            using var rightAlign = new StringFormat { Alignment = StringAlignment.Far };

            for (int i = 0; i < 10; i++)
            {
                bool hasEntry = i < scores.Count;
                string value = hasEntry ? scores[i].ToString() : "—";

                bool highlightThisRow = !alreadyHighlighted && hasEntry && highlightScore.HasValue && scores[i] == highlightScore.Value;
                if (highlightThisRow) alreadyHighlighted = true;

                var brush = highlightThisRow ? goldBrush : grayBrush;
                float y = top + i * rowHeight;

                g.DrawString($"{i + 1}.", rowFont, brush, left, y);

                var valueRect = new RectangleF(left + numberColumnWidth, y, tableWidth - numberColumnWidth, rowHeight);
                g.DrawString(value, rowFont, brush, valueRect, rightAlign);
            }
        }
    }
}
