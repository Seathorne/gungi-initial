﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gungi_cs
{
    class Piece
    {
        private int color, type, elevated_tier;
        private int[] location;
        private int[,,] moveset, moves, attacks;
        private int[,] line_of_sight, attack_line_of_sight;
        private bool in_hand, on_board, top, in_leading_sight;

        public Piece(int _player_color, int _piece_number)
        {
            color = _player_color;
            type = _piece_number;

            location = new int[3];
            moveset = new int[P.TM, P.RM, P.FM];

            PlaceInHand();
        }

        private void SetLocation(int _tier, int _rank, int _file)
        {
            location[P.T] = _tier;
            location[P.R] = _rank;
            location[P.F] = _file;

            if (_tier >= 0 && _tier < P.TM && _rank >= 0 && _rank < P.RM && _file >= 0 && _file < P.FM)
            {
                PlaceOnBoard();
                SetMoveset();
            }
            else if (_tier == P.HAND && _rank == P.HAND && _file == P.HAND)
            {
                PlaceInHand();
            }
            else
            {
                PlaceInCaptured();
            }
        }

        private void PlaceInHand()
        {
            in_hand = true;
            on_board = false;
            top = false;
        }

        private void PlaceOnBoard()
        {
            in_hand = false;
            on_board = true;
            top = true;
        }

        private void PlaceInCaptured()
        {
            in_hand = false;
            on_board = false;
            top = false;
        }

        public bool Drop(int _tier, int _rank, int _file)
        {
            if (in_hand)//and legal drop move
            {
                SetLocation(_tier, _rank, _file);
                return true;
            }
            return false;
        }

        public bool BoardMove(int _tier, int _rank, int _file)
        {
            if (on_board)//and legal move
            {
                SetLocation(_tier, _rank, _file);
                return true;
            }
            return false;
        }

        public bool Capture()
        {
            if (on_board)
            {
                SetLocation(-1, -1, -1);
                return true;
            }
            return false;
        }

        public int Color()
        {
            return color;
        }

        public int Type()
        {
            return type;
        }

        public int Sym()
        {
            return type * (color == P.BLACK ? -1 : 1);
        }

        public int[] Location()
        {
            return location;
        }

        public void SetLeadingPieceInSight(bool _in_sight)
        {
            in_leading_sight = _in_sight;
        }

        public bool LeadingPieceInSight()
        {
            return in_leading_sight;
        }

        public void SetElevatedTier(int _elevated_tier)
        {
            elevated_tier = _elevated_tier;
        }

        public int ElevatedTier()
        {
            return elevated_tier;
        }

        public int T()
        {
            return location[P.T];
        }

        public int R()
        {
            return location[P.R];
        }

        public int F()
        {
            return location[P.F];
        }

        private void SetMoveset()
        {
            if (!on_board) return;

            int shift_r = P.OC_R - location[P.R];
            int shift_f = P.OC_F - location[P.F];

            for (int t = 0; t < P.TM; t++)
            {
                for (int r = 0; r < P.RM; r++)
                {
                    for (int f = 0; f < P.FM; f++)
                    {
                        moveset[t, r, f] = Constants.GetMoves(type)[t, r + shift_r, f + shift_f];
                    }
                }
            }
        }

        public int[,,] Moveset()
        {
            return moveset;
        }

        public void SetLineOfSight(int[,] _line_of_sight)
        {
            line_of_sight = _line_of_sight;
        }

        public void SetAttackLineOfSight(int[,] _attack_line_of_sight)
        {
            attack_line_of_sight = _attack_line_of_sight;
        }

        public int[,] LineOfSight()
        {
            return line_of_sight;
        }

        public int[,] AttackLineOfSight()
        {
            if (!JumpAttacks())
            {
                return line_of_sight;
            }
            return attack_line_of_sight;
        }

        public void SetMoves(int[,,] _moves)
        {
            moves = _moves;
        }

        public int[,,] Moves()
        {
            return moves;
        }

        public void SetAttacks(int[,,] _attacks)
        {
            attacks = _attacks;
        }

        public int[,,] Attacks()
        {
            return attacks;
        }

        public bool InHand()
        {
            return in_hand;
        }

        public bool OnBoard()
        {
            return on_board;
        }

        public void SetTop(bool _top)
        {
            top = _top;
        }

        public bool Top()
        {
            return top;
        }

        public bool Alive()
        {
            return in_hand || on_board;
        }

        public bool IsUnstackable()
        {
            return (type == P.MAR);
        }

        public bool Leads()
        {
            return (type == P.LIE);
        }

        public bool Elevates()
        {
            return (type == P.FOR);
        }

        public bool JumpAttacks()
        {
            return (type == P.CAN);
        }

        public bool Teleports()
        {
            return (type == P.ARC || type == P.KNI);
        }

        public bool QuickStarts()
        {
            return false;// (type == P.PAW);
        }

        override
        public String ToString()
        {
            return Sym() + "";
        }
    }
}
