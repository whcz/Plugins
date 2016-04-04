﻿using System;
using Ctor.Resources;
using WHOkna;

namespace Ctor.Models
{
    /// <summary>
    /// Objekt plochy v rámu.
    /// </summary>
    public class FrameArea : Area
    {
        private readonly Frame _parent;

        internal FrameArea(IArea area, Frame parent)
            : base(area)
        {
            _parent = parent;
        }

        /// <summary>
        /// Vloží křídlo do pole. 
        /// Pokud pole není prázdné, nebo se nepodaří křídlo vložit, vyhodí <see cref="ModelException"/>.
        /// </summary>
        public Sash InsertSash()
        {
            CheckInvalidation();

            if (this.IsEmpty)
            {
                _area.AddChild(EProfileType.tSkrz, null);
                ISash sash = _area.FindSash();
                if (sash != null)
                {
                    return new Sash(sash, _parent);
                }
            }

            throw new ModelException(Strings.CannotInsertSash);
        }

        #region Insert false mullion

        /// <summary>
        /// Vloží štulp v barvě rámu do tohoto pole na střed. 
        /// Po vložení štulpu je tato oblast zneplatněna.
        /// </summary>
        /// <param name="nrArt">Číslo artiklu štulpu.</param>
        /// <param name="isLeftSide">Zda-li je štulp levý.</param>
        public void InsertFalseMullion(string nrArt, bool isLeftSide)
        {
            this.InsertFalseMullion(nrArt, isLeftSide, 0.5f);
        }

        /// <summary>
        /// Vloží štulp v barvě rámu do tohoto pole. 
        /// Po vložení štulpu je tato oblast zneplatněna.
        /// </summary>
        /// <param name="nrArt">Číslo artiklu štulpu.</param>
        /// <param name="isLeftSide">Zda-li je štulp levý.</param>
        /// <param name="dimX">Relativní souřadnice v ose X vzhledem k šíři pole.</param>
        public void InsertFalseMullion(string nrArt, bool isLeftSide, float dimX)
        {
            this.InsertFalseMullion(nrArt, isLeftSide, dimX, _parent.Data.Color);
        }

        /// <summary>
        /// Vloží štulp do tohoto pole. 
        /// Po vložení štulpu je tato oblast zneplatněna.
        /// </summary>
        /// <param name="nrArt">Číslo artiklu štulpu.</param>
        /// <param name="isLeftSide">Zda-li je štulp levý.</param>
        /// <param name="dimX">Relativní souřadnice v ose X vzhledem k šíři pole.</param>
        /// <param name="color">ID barvy.</param>
        public void InsertFalseMullion(string nrArt, bool isLeftSide, float dimX, int color)
        {
            CheckInvalidation();

            if (dimX <= 0 || 1 <= dimX) throw new ArgumentOutOfRangeException();

            var origRectangle = _area.Rectangle;

            var parameters = Parameters.ForFalseMullion(nrArt, color, isLeftSide);
            var insertionPoint = new System.Drawing.PointF();
            insertionPoint.X = _area.Rectangle.X + (_area.Rectangle.Width * dimX);
            insertionPoint.Y = _area.Rectangle.Y + (_area.Rectangle.Height * 0.5f);

            _area.AddBar(EProfileType.tPrzymyk, EDir.dLeft, insertionPoint, parameters);

            var top = _area.TopObject;
            if (top.Update(true))
            {
                top.CheckPoint();
                top.Invalidate();

                this.Invalidate();

                var area1 = _parent.GetArea((origRectangle.Left + insertionPoint.X) / 2, insertionPoint.Y);
                var area2 = _parent.GetArea((origRectangle.Right + insertionPoint.X) / 2, insertionPoint.Y);

                area1.InsertSash();
                area2.InsertSash();
            }
            else
            {
                top.Undo(Strings.CannotInsertFalseMullion);
                top.Invalidate();
                throw new ModelException(Strings.CannotInsertFalseMullion);
            }
        }

        #endregion

        #region Insert mullion

        /// <summary>
        /// Vloží horizontální sloupek v barvě rámu do tohoto pole na střed.
        /// Po vložení sloupku je tato oblast zneplatněna.
        /// </summary>
        /// <param name="nrArt">Číslo artiklu sloupku.</param>
        public void InsertHorizontalMullion(string nrArt)
        {
            this.InsertHorizontalMullion(nrArt, 0.5f);
        }

        /// <summary>
        /// Vloží horizontální sloupek v barvě rámu do tohoto pole.
        /// Po vložení sloupku je tato oblast zneplatněna.
        /// </summary>
        /// <param name="nrArt">Číslo artiklu sloupku.</param>
        /// <param name="dimY">Relativní souřadnice v ose Y vzhledem k výšce pole.</param>
        public void InsertHorizontalMullion(string nrArt, float dimY)
        {
            this.InsertHorizontalMullion(nrArt, dimY, _parent.Data.Color);
        }

        /// <summary>
        /// Vloží horizontální sloupek do tohoto pole.
        /// Po vložení sloupku je tato oblast zneplatněna.
        /// </summary>
        /// <param name="nrArt">Číslo artiklu sloupku.</param>
        /// <param name="dimY">Relativní souřadnice v ose Y vzhledem k výšce pole.</param>
        /// <param name="color">ID barvy.</param>
        public void InsertHorizontalMullion(string nrArt, float dimY, int color)
        {
            this.InsertMullionCore(nrArt, dimY, color, EDir.dLeft);
        }

        /// <summary>
        /// Vloží vertikální sloupek v barvě rámu do tohoto pole na střed.
        /// Po vložení sloupku je tato oblast zneplatněna.
        /// </summary>
        /// <param name="nrArt">Číslo artiklu sloupku.</param>
        public void InsertVerticalMullion(string nrArt)
        {
            this.InsertVerticalMullion(nrArt, 0.5f);
        }

        /// <summary>
        /// Vloží vertikální sloupek v barvě rámu do tohoto pole.
        /// Po vložení sloupku je tato oblast zneplatněna.
        /// </summary>
        /// <param name="nrArt">Číslo artiklu sloupku.</param>
        /// <param name="dimX">Relativní souřadnice v ose X vzhledem k šíři pole.</param>
        public void InsertVerticalMullion(string nrArt, float dimX)
        {
            this.InsertVerticalMullion(nrArt, dimX, _parent.Data.Color);
        }

        /// <summary>
        /// Vloží vertikální sloupek do tohoto pole.
        /// Po vložení sloupku je tato oblast zneplatněna.
        /// </summary>
        /// <param name="nrArt">Číslo artiklu sloupku.</param>
        /// <param name="dimX">Relativní souřadnice v ose X vzhledem k šíři pole.</param>
        /// <param name="color">ID barvy.</param>
        public void InsertVerticalMullion(string nrArt, float dimX, int color)
        {
            this.InsertMullionCore(nrArt, dimX, color, EDir.dTop);
        }

        private void InsertMullionCore(string nrArt, float dim, int color, EDir direction)
        {
            CheckInvalidation();

            if (dim <= 0 || 1 <= dim) throw new ArgumentOutOfRangeException();

            var parameters = Parameters.ForMullion(nrArt, color);
            var insertionPoint = new System.Drawing.PointF();
            float dimX = 0.5f, dimY = 0.5f;
            switch (direction)
            {
                case EDir.dTop:
                    dimX = dim;
                    break;

                case EDir.dLeft:
                    dimY = dim;
                    break;

                default:
                    throw new ModelException(string.Format(Strings.InvalidMullionOrientation, direction));
            }
            insertionPoint.X = _area.Rectangle.X + (_area.Rectangle.Width * dimX);
            insertionPoint.Y = _area.Rectangle.Y + (_area.Rectangle.Height * dimY);

            _area.AddBar(EProfileType.tSlupek, direction, insertionPoint, parameters);

            var top = _area.TopObject;
            if (top.Update(true))
            {
                top.CheckPoint();
                top.Invalidate();

                this.Invalidate();
            }
            else
            {
                top.Undo(Strings.CannotInsertMullion);
                top.Invalidate();
                throw new ModelException(Strings.CannotInsertMullion);
            }
        }

        #endregion
    }
}
