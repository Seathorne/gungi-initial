﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gungi_cs
{
    class Array
    {
        // Board State
        bool setup_phase;
        int[,,] board;
        int[,] board_top;
        int[][,,] board_open, board_in_check;

        // Modifiers
        int[,] lt_gen_sight, fort_range;

        // Valid Moves
        HashSet<Piece> board_pieces, hand_pieces, top_pieces,
            marshal_pieces, leading_pieces, elevating_pieces, jumping_pieces, teleporting_pieces, hand_pawn_pieces;
        Piece selected_piece;

        // Check
        bool[] in_check;
        bool[] in_checkmate;
        int[] check_count;

        public Array()
        {
            setup_phase = true;
            selected_piece = null;

            Clear();
        }

        public void Clear()
        {
            board = new int[P.TM, P.RM, P.FM];
            board_top = new int[P.RM, P.FM];
            board_open = new int[][,,] { new int[P.TM, P.RM, P.FM], new int[P.TM, P.RM, P.FM], new int[P.TM, P.RM, P.FM] };
            board_in_check = new int[][,,] { new int[P.TM, P.RM, P.FM], new int[P.TM, P.RM, P.FM], new int[P.TM, P.RM, P.FM] };

            lt_gen_sight = new int[P.RM, P.FM];
            fort_range = new int[P.RM, P.FM];

            board_pieces = new HashSet<Piece>();
            hand_pieces = new HashSet<Piece>();
            top_pieces = new HashSet<Piece>();
            marshal_pieces = new HashSet<Piece>();
            leading_pieces = new HashSet<Piece>();
            elevating_pieces = new HashSet<Piece>();
            jumping_pieces = new HashSet<Piece>();
            teleporting_pieces = new HashSet<Piece>();
            hand_pawn_pieces = new HashSet<Piece>();

            in_check = new bool[2];
            in_checkmate = new bool[2];
            check_count = new int[2];
        }

        public void DuringCheckMateClear()
        {
            board = new int[P.TM, P.RM, P.FM];
            board_top = new int[P.RM, P.FM];
            board_open = new int[][,,] { new int[P.TM, P.RM, P.FM], new int[P.TM, P.RM, P.FM], new int[P.TM, P.RM, P.FM] };
            board_in_check = new int[][,,] { new int[P.TM, P.RM, P.FM], new int[P.TM, P.RM, P.FM], new int[P.TM, P.RM, P.FM] };

            lt_gen_sight = new int[P.RM, P.FM];
            fort_range = new int[P.RM, P.FM];

            board_pieces = new HashSet<Piece>();
            hand_pieces = new HashSet<Piece>();
            top_pieces = new HashSet<Piece>();
            marshal_pieces = new HashSet<Piece>();
            leading_pieces = new HashSet<Piece>();
            elevating_pieces = new HashSet<Piece>();
            jumping_pieces = new HashSet<Piece>();
            teleporting_pieces = new HashSet<Piece>();
            hand_pawn_pieces = new HashSet<Piece>();
        }

        public void ClearPiece(Piece _piece)
        {
            int[,,] blank = new int[P.TM, P.RM, P.FM];
            int[,] blank_los = new int[P.RM, P.FM];

            _piece.SetTop(false);
            _piece.SetLeadingPieceInSight(false);
            _piece.SetElevatingTier(_piece.T());

            _piece.SetLineOfSight(blank_los);
            _piece.SetAttackLineOfSight(blank_los);

            _piece.SetMoves(blank);
            _piece.SetAttacks(blank);
        }

        public void Update(HashSet<Piece> _board_pieces, HashSet<Piece> _hand_pieces)
        {
            Clear();
            selected_piece = null;

            board_pieces = _board_pieces;
            hand_pieces = _hand_pieces;

            UpdateBoardStates();
            UpdatePieces();
            UpdatePiecesDrops();

            if (!setup_phase)
            {
                UpdateCheck();
                UpdateCheckMate();
            }
        }

        public void DuringCheckMateUpdate(HashSet<Piece> _board_pieces, HashSet<Piece> _hand_pieces)
        {
            DuringCheckMateClear();

            board_pieces = _board_pieces;
            hand_pieces = _hand_pieces;

            UpdateBoardStates();
            UpdatePieces();
        }

        private void FakeUpdate(HashSet<Piece> _board_pieces, HashSet<Piece> _hand_pieces)
        {
            Clear();

            board_pieces = _board_pieces;
            hand_pieces = _hand_pieces;

            UpdateBoardStates();
            UpdatePieces();

            UpdateCheck();
        }

        private void UpdateCheck()
        {
            in_check = new bool[2];
            check_count = new int[2];

            foreach (Piece p in marshal_pieces)
            {
                check_count[p.PlayerColor()] = CheckCount(p);
                in_check[p.PlayerColor()] = (check_count[p.PlayerColor()] > 0);
            }
        }

        private void UpdateCheckMate()
        {
            in_checkmate = new bool[2];

            if (in_check[P.BLACK])
            {
                CheckCheckmate(P.BLACK);
            }
            if (in_check[P.WHITE])
            {
                CheckCheckmate(P.WHITE);
            }
        }

        public int CheckCount(int _player_color)
        {
            return check_count[_player_color];
        }

        public bool IsInCheck(int _player_color)
        {
            return in_check[_player_color];
        }

        public bool IsInCheckMate(int _player_color)
        {
            return in_checkmate[_player_color];
        }

        public void Select(Piece _piece)
        {
            selected_piece = _piece;
            if (!setup_phase && hand_pawn_pieces.Contains(selected_piece))
            {
                UpdatePawnDrops(_piece);
            }
        }

        public void Deselect()
        {
            selected_piece = null;
        }

        public void SetupDone()
        {
            setup_phase = false;
        }

        private void UpdateBoardStates()
        {
            foreach (Piece p in board_pieces)
            {
                ClearPiece(p);
                board[p.T(), p.R(), p.F()] = p.Sym();                       // Construct the board array.

                if (p.Type() == P.MAR)                                      // Add special pieces to their own sets.
                {
                    marshal_pieces.Add(p);
                }
            }

            foreach (Piece p in hand_pieces)
            {
                if (p.Type() == P.PAW)
                {
                    hand_pawn_pieces.Add(p);
                }
            }

            for (int r = 0; r < P.RM; r++)
            {
                for (int f = 0; f < P.RM; f++)
                {
                    bool unstackable = false;
                    foreach (Piece p in marshal_pieces)
                    {
                        unstackable = (r == p.R() && f == p.F());
                        if (unstackable)
                        {
                            board_top[r, f] = p.Sym();                      // If a marshal is found, it is the top piece, and the rest of the stack is not open.
                            break;
                        }
                    }

                    for (int t = P.TM - 1; t >= 0; t--)
                    {
                        int cell = board[t, r, f];

                        if (!unstackable && !Empty(cell))
                        {
                            board_top[r, f] = cell;                         // If a piece is found, it is the top piece.
                            if (t < P.TM - 1)
                            {
                                board_open[P.EMPTY][t + 1, r, f] = 1;       // If this piece is in tier 1 or 2, make the cell above it open.
                            }
                            break;
                        }
                        else if (Empty(cell) && t == 0)
                        {
                            board_open[P.EMPTY][t, r, f] = 1;               // If a spot in tier 1 is empty, make it open.
                        }
                    }
                }
            }

            foreach (Piece p in board_pieces)
            {
                if (p.Sym() == board_top[p.R(), p.F()] && p.T() == StackHeight(p.R(), p.F()) - 1)
                {
                    top_pieces.Add(p);                                      // Add top pieces to their own set.
                    p.SetTop(true);
                    board_open[p.PlayerColor()][p.T(), p.R(), p.F()] = 1;   // Add piece to black or white board.

                    if (p.Leads())
                    {
                        leading_pieces.Add(p);
                    }
                    if (p.Elevates())
                    {
                        elevating_pieces.Add(p);
                    }
                    if (p.JumpAttacks())
                    {
                        jumping_pieces.Add(p);
                    }
                    if (p.Teleports())
                    {
                        teleporting_pieces.Add(p);
                    }
                }
                else
                {
                    p.SetTop(false);
                }
            }
        }

        private int CheckCount(Piece p)
        {
            return (board_in_check[p.PlayerColor()][p.T(), p.R(), p.F()]);
        }
        
        private void CheckCheckmate(int _checked_player_color)
        {
            in_checkmate[_checked_player_color] = true;

            foreach (Piece p in board_pieces)
            {
                if (p.PlayerColor() == _checked_player_color)
                {
                    for (int t = 0; t < P.TM; t++)
                    {
                        for (int r = 0; r < P.RM; r++)
                        {
                            for (int f = 0; f < P.FM; f++)
                            {
                                if (p.CanMoveTo(new int[] { t, r, f }))
                                {
                                    if (!IsInCheckAfterFakeMove(p, new int[] { t, r, f }))
                                    {
                                        Console.WriteLine(P.ConvertColor(_checked_player_color) + " may escape check by moving [" + p.Char() + "] from " + p.LocationStringRFT() + " to " + (r + 1) + "-" + (f + 1) + "-" + (t + 1) + ".");
                                        in_checkmate[_checked_player_color] = false;
                                        //return;
                                    }
                                }
                                else if (p.CanAttackTo(new int[] { t, r, f }))
                                {
                                    if (!IsInCheckAfterFakeAttack(p, new int[] { t, r, f }))
                                    {
                                        Console.WriteLine(P.ConvertColor(_checked_player_color) + " may escape check by attacking [" + p.Char() + "] from " + p.LocationStringRFT() + " to " + (r + 1) + "-" + (f + 1) + "-" + (t + 1) + ".");
                                        in_checkmate[_checked_player_color] = false;
                                        //return;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            bool only_has_pawns = true;
            foreach (Piece p in hand_pieces)
            {
                if (p.PlayerColor() == _checked_player_color && p.Type() != P.PAW)
                {
                    only_has_pawns = false;
                    break;
                }
            }
            Piece hand_piece = null;
            foreach (Piece p in hand_pieces)
            {
                if (only_has_pawns && p.Type() == P.PAW)
                {
                    hand_piece = p;
                    break;
                }
                else if (p.PlayerColor() == _checked_player_color && p.Type() != P.PAW)
                {
                    hand_piece = p;
                    break;
                }
            }
            if (hand_piece != null)
            {
                if (hand_piece.PlayerColor() == _checked_player_color)
                {
                    for (int t = 0; t < P.TM; t++)
                    {
                        for (int r = 0; r < P.RM; r++)
                        {
                            for (int f = 0; f < P.FM; f++)
                            {
                                if (hand_piece.CanDropTo(new int[] { t, r, f }))
                                {
                                    if (!IsInCheckAfterFakeDrop(_checked_player_color, hand_piece, new int[] { t, r, f }))
                                    {
                                        Console.WriteLine(P.ConvertColor(hand_piece.PlayerColor()) + " may escape check by dropping a piece onto " + (r + 1) + "-" + (f + 1) + "-" + (t + 1) + ".");
                                        in_checkmate[_checked_player_color] = false;
                                        //return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public bool IsOutOfCheckAfterMove(Piece _p, int[] _fake_location)
        {
            return !IsInCheckAfterFakeMove(_p, _fake_location);
        }

        public bool IsOutOfCheckAfterAttack(Piece _p, int[] _fake_location)
        {
            return !IsInCheckAfterFakeAttack(_p, _fake_location);
        }

        public bool IsOutOfCheckAfterDrop(Piece _p, int[] _fake_location)
        {
            return !IsInCheckAfterFakeDrop(_p.PlayerColor(), _p, _fake_location);
        }

        private bool IsInCheckAfterFakeMove(Piece _p, int[] _fake_location)
        {
            int _t = _fake_location[P.Ti];
            int _r = _fake_location[P.Ri];
            int _f = _fake_location[P.Fi];

            int[] real_location = new int[] { _p.T(), _p.R(), _p.F() };

            _p.MoveTo(_fake_location);

            Array fake_array = new Array();
            fake_array.FakeUpdate(board_pieces, hand_pieces);
            bool fake_check = fake_array.in_check[_p.PlayerColor()];
            fake_array = null;

            _p.MoveTo(real_location);
            DuringCheckMateUpdate(board_pieces, hand_pieces);

            return (fake_check);
        }

        private bool IsInCheckAfterFakeAttack(Piece _p, int[] _fake_location)
        {
            int _t = _fake_location[P.Ti];
            int _r = _fake_location[P.Ri];
            int _f = _fake_location[P.Fi];

            int[] real_location = new int[] { _p.T(), _p.R(), _p.F() };
            Piece attacked_piece = null;
            HashSet<Piece> fake_board_pieces = new HashSet<Piece>();

            foreach (Piece p in board_pieces)
            {
                if (p.T() == _t && p.R() == _r && p.F() == _f)
                {
                    attacked_piece = p;
                }
                else
                {
                    fake_board_pieces.Add(p);
                }
            }

            attacked_piece.GetsAttacked();
            _p.MoveTo(_fake_location);

            Array fake_array = new Array();
            fake_array.FakeUpdate(fake_board_pieces, hand_pieces);
            bool fake_check = fake_array.in_check[_p.PlayerColor()];
            fake_array = null;

            _p.MoveTo(real_location);
            attacked_piece.MoveTo(_fake_location);
            DuringCheckMateUpdate(board_pieces, hand_pieces);

            return (fake_check);
        }

        private bool IsInCheckAfterFakeDrop(int _player_color_to_check, Piece _drop_piece, int[] _fake_location)
        {
            int _t = _fake_location[P.Ti];
            int _r = _fake_location[P.Ri];
            int _f = _fake_location[P.Fi];

            HashSet<Piece> fake_board_pieces = new HashSet<Piece>();
            HashSet<Piece> fake_hand_pieces = new HashSet<Piece>();

            _drop_piece.MoveTo(_fake_location);
            foreach (Piece p in board_pieces)
            {
                fake_board_pieces.Add(p);
            }
            fake_board_pieces.Add(_drop_piece);
            fake_hand_pieces.Remove(_drop_piece);

            Array fake_array = new Array();
            fake_array.FakeUpdate(fake_board_pieces, fake_hand_pieces);
            bool fake_check = fake_array.in_check[_player_color_to_check];
            fake_array = null;

            _drop_piece.PlaceInHand();
            DuringCheckMateUpdate(board_pieces, hand_pieces);
            UpdateDrops(_drop_piece);

            return (fake_check);
        }

        private void UpdatePieces()
        {
            foreach (Piece p in board_pieces)
            {
                UpdateLineOfSight(p);
                UpdateInLeadingSight(p);
                if (p.Type() == P.LIE)
                {
                    UpdateExtraDiagSight(p);
                }

                foreach (Piece e_p in elevating_pieces)
                {
                    UpdateMoves(e_p);
                }

                if (!elevating_pieces.Contains(p))
                {
                    UpdateElevation(p);
                    UpdateMoves(p);
                }

                UpdateCheckedPieces(p);
            }
        }

        private void UpdatePiecesDrops()
        {
            foreach (Piece p in hand_pieces)
            {
                UpdateDrops(p);
            }
        }

        private void UpdateCheckedPieces(Piece _p)
        {
            for (int t = 0; t < P.TM; t++)
            {
                for (int r = 0; r < P.RM; r++)
                {
                    for (int f = 0; f < P.FM; f++)
                    {
                        board_in_check[1 - _p.PlayerColor()][t, r, f] += _p.Attacks()[t, r, f];
                    }
                }
            }
        }

        private void UpdateElevation(Piece _piece)
        {
            int e_tier = 0;

            foreach (Piece e_p in elevating_pieces)
            {
                if (e_p.PlayerColor() == _piece.PlayerColor() && e_p.Moveset()[_piece.T(), _piece.R(), _piece.F()] == 1)
                {
                    e_tier = Math.Min(P.TM - 1, Math.Max(e_tier, e_p.T() + 1));
                }
            }

            _piece.SetElevatingTier(e_tier);
        }

        private void UpdateDrops(Piece _piece)
        {
            int[,,] drops = new int[P.TM, P.RM, P.FM];

            for (int t = 0; t < P.TM; t++)
            {
                for (int r = 0; r < P.RM; r++)
                {
                    for (int f = 0; f < P.FM; f++)
                    {
                        drops[t, r, f] = board_open[P.EMPTY][t, r, f];
                        if (setup_phase)
                        {
                            if (_piece.PlayerColor() == P.BLACK && r < P.RM - P.NUM_SETUP_RANKS)
                            {
                                drops[t, r, f] = 0;
                            }
                            else if (_piece.PlayerColor() == P.WHITE && r >= P.NUM_SETUP_RANKS)
                            {
                                drops[t, r, f] = 0;
                            }
                        }
                    }
                }
            }

            _piece.SetDrops(drops);
        }

        private void UpdatePawnDrops(Piece _piece)
        {
            int[,,] drops = _piece.Drops();

            Piece enemy_marshal = null;
            foreach (Piece p in marshal_pieces)
            {
                if (p.PlayerColor() == (1 - _piece.PlayerColor()))
                {
                    enemy_marshal = p;
                }
            }

            for (int t = 0; t < P.TM; t++)
            {
                for (int r = 0; r < P.RM; r++)
                {
                    for (int f = 0; f < P.FM; f++)
                    {
                        drops[t, r, f] = _piece.Drops()[t, r, f];
                        if (r >= Math.Max(0, enemy_marshal.R() - 1) && r <= Math.Min(P.RM - 1, enemy_marshal.R() + 1) && f >= Math.Max(0, enemy_marshal.F() - 1) && f <= Math.Min(P.FM - 1, enemy_marshal.F() + 1))
                        {
                            if (drops[t, r, f] == 1)
                            {
                                if (IsInCheckAfterFakeDrop(enemy_marshal.PlayerColor(), _piece, new int[] { t, r, f }))
                                {
                                    drops[t, r, f] = 0;
                                }
                            }
                        }
                    }
                }
            }

            _piece.SetDrops(drops);
        }

        private void UpdateExtraDiagSight(Piece _piece)
        {
            int[,] line_of_sight = _piece.LineOfSight();

            int sign = (_piece.PlayerColor() == P.BLACK) ? -1 : 1;
            int forward_dir = (_piece.PlayerColor() == P.BLACK) ? P.UP : P.DOWN;

            int forward_sight = SightLength(forward_dir, _piece.R(), _piece.F(), out int x1, out int x2);
            if (forward_sight >= 2)
            {
                bool forward_open = (StackHeight(_piece.R() + sign, _piece.F()) == 0);
                if (forward_open)
                {
                    int left_sight = SightLength(P.LEFT, _piece.R(), _piece.F(), out int x3, out int x4);
                    if (left_sight >= 1)
                    {
                        line_of_sight[_piece.R() + 2 * sign, _piece.F() - 1] = 1;
                    }

                    int right_sight = SightLength(P.RIGHT, _piece.R(), _piece.F(), out int x5, out int x6);
                    if (right_sight >= 1)
                    {
                        line_of_sight[_piece.R() + 2 * sign, _piece.F() + 1] = 1;
                    }

                    _piece.SetLineOfSight(line_of_sight);
                    _piece.SetAttackLineOfSight(line_of_sight);
                }
            }
        }

        private void UpdateMoves(Piece _piece)
        {
            int[,,] moves = new int[P.TM, P.RM, P.FM];
            int[,,] attacks = new int[P.TM, P.RM, P.FM];

            if (_piece.OnBoard())
            {
                for (int t = 0; t < P.TM; t++)
                {
                    for (int r = 0; r < P.RM; r++)
                    {
                        for (int f = 0; f < P.FM; f++)
                        {
                            if (_piece.LeadingPieceInSight())
                            {
                                for (int m_t = 0; m_t < P.TM; m_t++)
                                {
                                    moves[t, r, f] |= _piece.Moveset()[m_t, r, f];
                                    attacks[t, r, f] |= _piece.Moveset()[m_t, r, f];
                                }
                            }
                            else
                            {
                                moves[t, r, f] = _piece.Moveset()[_piece.ElevatedTier(), r, f];
                                if (_piece.JumpAttacks()) attacks[t, r, f] |= _piece.Moveset()[P.TM - 1, r, f];
                                else attacks[t, r, f] = _piece.Moveset()[_piece.ElevatedTier(), r, f];
                            }

                            moves[t, r, f] &= board_open[P.EMPTY][t, r, f] & (_piece.Teleports() ? 1 : _piece.LineOfSight()[r, f]);
                            attacks[t, r, f] &= board_open[1 - _piece.PlayerColor()][t, r, f] & (_piece.Teleports() ? 1 : _piece.AttackLineOfSight()[r, f]);
                        }
                    }
                }
            }

            _piece.SetMoves(moves);
            _piece.SetAttacks(attacks);
        }

        private void UpdateLineOfSight(Piece _piece)
        {
            int[,] line_of_sight = new int[P.RM, P.FM];
            int[,] attack_line_of_sight = new int[P.RM, P.FM];

            for (int dir = 0; dir < P.NUM_DIR; dir++)
            {
                bool seen_piece = false;
                int jump_count = 0;

                int sight_length = SightLength(dir, _piece.R(), _piece.F(), out int r_sign, out int f_sign);
                for (int i = 1; i <= sight_length; i++)
                {
                    int r_ = _piece.R() + r_sign * i;
                    int f_ = _piece.F() + f_sign * i;

                    if (!seen_piece)
                    {
                        line_of_sight[r_, f_] = 1;
                        if (!Empty(board_top[r_, f_]))
                        {
                            seen_piece = true;
                        }
                    }

                    if (_piece.JumpAttacks())
                    {
                        if (jump_count == 1)
                        {
                            attack_line_of_sight[r_, f_] = 1;
                        }
                        if (!Empty(board_top[r_, f_]))
                        {
                            jump_count++;
                        }
                    }
                }
            }

            _piece.SetLineOfSight(line_of_sight);
            _piece.SetAttackLineOfSight(attack_line_of_sight);
        }

        private void UpdateInLeadingSight(Piece _piece)
        {
            bool in_sight = false;
            foreach (Piece l_p in leading_pieces)
            {
                if (_piece != l_p)
                {
                    in_sight |= (_piece.LineOfSight()[l_p.R(), l_p.F()] == 1 && _piece.PlayerColor() == l_p.PlayerColor());
                }
            }
            _piece.SetLeadingPieceInSight(in_sight);
        }

        private int SightLength(int _dir, int _rank, int _file, out int _r, out int _f)
        {
            _r = 0;
            _f = 0;
            switch (_dir)
            {
                case P.UP_LEFT:
                    _r = -1; _f = -1;
                    return Math.Min(_rank, _file);
                case P.UP:
                    _r = -1;
                    return _rank;
                case P.UP_RIGHT:
                    _r = -1; _f = 1;
                    return Math.Min(_rank, P.FM - 1 - _file);
                case P.LEFT:
                    _f = -1;
                    return _file;
                case P.RIGHT:
                    _f = 1;
                    return P.FM - 1 - _file;
                case P.DOWN_LEFT:
                    _r = 1; _f = -1;
                    return Math.Min(P.RM - 1 - _rank, _file);
                case P.DOWN:
                    _r = 1;
                    return P.RM - 1 - _rank;
                case P.DOWN_RIGHT:
                    _r = 1; _f = 1;
                    return Math.Min(P.RM - 1 - _rank, P.FM - 1 - _file);
                default:
                    return 0;
            }
        }

        private bool Empty(int _piece)
        {
            return (_piece == P.EMP);
        }

        public void PrintBoard(int _modifier)
        {
            HashSet<String> locations = new HashSet<String>();
            if (_modifier == P.WHITE || _modifier == P.BLACK)
            {
                foreach (Piece p in top_pieces)
                {
                    if (p.PlayerColor() == _modifier)
                    {
                        locations.Add(p.LocationStringRFT());
                    }
                }
            }

            String ret = "    1   2   3   4   5   6   7   8   9  \n";
            for (int r = P.RM - 1; r >= 0; r--)
            {
                ret += "  ";
                for (int f = 0; f < P.FM; f++)
                {
                    ret += "·";
                    for (int t = 0; t < P.TM; t++)
                    {
                        if (selected_piece != null)
                        {
                            if (selected_piece.IsInLocation(t, r, f))
                            {
                                ret += "%";
                            }
                            else if (selected_piece.InHand() && selected_piece.Drops()[t, r, f] == 1)
                            {
                                ret += "o";
                            }
                            else if (selected_piece.OnBoard())
                            {
                                if (_modifier == P.WOULD_BE)
                                {
                                    if (selected_piece.WouldBeMoves()[t, r, f] == 1)
                                    {
                                        ret += "o";
                                    }
                                    else if (selected_piece.WouldBeAttacks()[t, r, f] == 1)
                                    {
                                        ret += "#";
                                    }
                                    else
                                    {
                                        ret += "-";
                                    }
                                }
                                else
                                {
                                    if (selected_piece.Moves()[t, r, f] == 1)
                                    {
                                        ret += "o";
                                    }
                                    else if (selected_piece.Attacks()[t, r, f] == 1)
                                    {
                                        ret += "#";
                                    }
                                    else
                                    {
                                        ret += "-";
                                    }
                                }
                            }
                            else
                            {
                                ret += "-";
                            }
                        }
                        else if (_modifier == P.WHITE || _modifier == P.BLACK)
                        {
                            if (locations.Contains((r + 1) + "-" + (f + 1) + "-" + (t + 1)))
                            {
                                ret += "%";
                            }
                            else
                            {
                                ret += '-';
                            }
                        }
                        else
                        {
                            ret += "-";
                        }
                    }
                }
                ret += "·\n";
                ret += (r + 1) + " ";
                for (int f = 0; f < P.FM; f++)
                {
                    ret += "|";
                    for (int t = 0; t < P.TM; t++)
                    {
                        ret += P.ConvertPiece(board[t, r, f]);
                    }
                }
                ret += "|\n";
            }
            ret += "  ·---·---·---·---·---·---·---·---·---·";
            Console.WriteLine(ret);
        }

        public int[] RandomDropLocation()
        {
            int loc_i = -1;
        Repeat:
            int valid_count = 0;
            for (int t = 0; t < P.TM; t++)
            {
                for (int r = 0; r < P.RM; r++)
                {
                    for (int f = 0; f < P.FM; f++)
                    {
                        if (selected_piece.Drops()[t, r, f] == 1) valid_count++;
                        if (valid_count == loc_i) return new int[] { t, r, f };
                    }
                }
            }

            loc_i = new Random().Next(0, valid_count) + 1;
            goto Repeat;
        }

        public void Print(String _word, int[,,] _array)
        {
            String ret = _word + "\n";
            for (int t = 0; t < _array.GetLength(0); t++)
            {
                for (int r = _array.GetLength(1) - 1; r >= 0; r--)
                {
                    for (int f = 0; f < _array.GetLength(2); f++)
                    {
                        if (_array[t, r, f] > 9)
                        {
                            ret += " ";
                        }
                        else if (_array[t, r, f] >= 0)
                        {
                            ret += "  ";
                        }
                        else if (_array[t, r, f] < -9)
                        {
                        }
                        else if (_array[t, r, f] < 0)
                        {
                            ret += " ";
                        }
                        if (_array[t, r, f] == 0)
                        {
                            ret += "· ";
                        }
                        else
                        {
                            ret += _array[t, r, f] + " ";
                        }
                    }
                    ret += '\n';
                }
                ret += '\n';
            }
            Console.WriteLine(ret);
        }

        public void Print(String _word, int[,] _array)
        {
            String ret = _word + "\n";
            for (int r = 0; r < _array.GetLength(0); r++)
            {
                for (int f = 0; f < _array.GetLength(1); f++)
                {
                    if (_array[r, f] > 9)
                    {
                        ret += " ";
                    }
                    else if (_array[r, f] >= 0)
                    {
                        ret += "  ";
                    }
                    else if (_array[r, f] < -9)
                    {
                    }
                    else if (_array[r, f] < 0)
                    {
                        ret += " ";
                    }
                    if (_array[r, f] == 0)
                    {
                        ret += "· ";
                    }
                    else
                    {
                        ret += _array[r, f] + " ";
                    }
                }
                ret += '\n';
            }
            Console.WriteLine(ret);
        }

        public int StackHeight(int _r, int _f)
        {
            int height = 0;
            for (int t = 0; t < P.TM; t++)
            {
                if (!Empty(board[t, _r, _f]))
                {
                    height++;
                }
                else
                {
                    return height;
                }
            }
            return height;
        }
        
    }
}
