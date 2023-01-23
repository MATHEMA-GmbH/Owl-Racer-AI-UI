using DocumentFormat.OpenXml.Presentation;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matlabs.OwlRacer.GameClient.States.Layout
{
    public static class layoutUtility
    {
        public static int screenWidth;
        public static int screenHeight;
        public static float scaleFactor;
        private static int sideSize;
        private static double borderFactor = 0.1;
        private static double columnSizeFactor = 0.25;
        private static double columnBorder = 0.025;
        private static double rowSizeFactor = 0.04;
        private static double rowBorder = 0.01;

        /*  
         *  The menustate layout is conceptualized as follows:
         *  (Measurements as % of screen-axis-length
         *  There is a 10% margin to the top and to the left
         *  rows are 5% of the screen height including a 1% border
         *
         *  There are up to 3 columns
         *  Each column has  a width of 25% of the screen's width 
         *  There is a 2.5% gap in between columns 1 and 2
         *  The same gap is between columns 2 and 3
         *
         *  The methods VectorPosXY, xValue and YValue
         *  are used for positioning elements on the screen
         *
         *  The methods widthPx and heightPx are used to scale
         *  sizes of non text elements 
         */ 
        
              
        

        public static Vector2 VectorPosXY(double columnNumber, double rowNumber)
        {
            int currentColumnNumber = (int)(Math.Floor(columnNumber));
            int xPos = (int)(screenWidth * (borderFactor + (columnBorder * currentColumnNumber) + columnSizeFactor * columnNumber));
            int yPos = (int)(screenHeight * (borderFactor + (rowSizeFactor + rowBorder) * rowNumber));
            return new Vector2(xPos, yPos);
        }
        public static int XValue(double columnNumber)
        {
            int xPos = (int)(screenWidth * (borderFactor + (columnBorder * Math.Floor(columnNumber) + columnSizeFactor * columnNumber)));
            return xPos;
        }

        public static int YValue(double rowNumber)
        {

            int yPos = (int)(screenHeight * (borderFactor + (rowSizeFactor + rowBorder) * rowNumber));
            return yPos;
        }


        //Methods to calculate element sizes based on shared column width and shared row height

        public static int widthPx(double columnNumber)
        {
            int interimResult = (int)(screenWidth * columnNumber * columnSizeFactor);
            return interimResult;
        }
        public static int heightPx(double rowNumber)
        {
            int interimResult = (int)(screenHeight * rowSizeFactor * rowNumber);
            return interimResult;
        }

        /*
         * Methods used to design the circle in the top right corner, which is used in the menustate
         * 
         * The distance between elements on the y axis is 20% of the circle radius
         *
         *Circle radius is set to be at 12.5% of the screen width/height depending
         *on which of the two is larger.
         *
         *The initial position of elements on the x axis is at 10% of the same base
         *size leading to a 2.5% margin
         *
         *Adding more than 3 elements requires adjusting the xPos value
         */

        public static Vector2 topRightVectorPosXY(int rowNumber)
        {
            sideSize = (int)(screenWidth * borderFactor);
            int xPos = (int)(screenWidth * (1 - borderFactor));
            int yPos = (int)(sideSize * borderFactor) + (int)(rowNumber * (double)sideSize * 0.2);
            return new Vector2(xPos, yPos);
        }

        public static int topRightXValue()
        {
            int xPos = (int)(screenWidth * (1 - borderFactor));
            return xPos;
        }
        public static int topRightYValue(int rowNumber)
        {
            sideSize = (int)(screenWidth * borderFactor);
            int xPos = (int)(screenWidth * (1 - borderFactor));
            int yPos = (int)(sideSize * borderFactor) + (int)(rowNumber * (double)sideSize * 0.2);
            return yPos;
        }


         /*
          * Methods used to design the circle in the bottom righ corner, which is used in the rankingstate and gamestate
          * 
          * The distance between elements on the y axis is 20% of the circle radius
          *
          * Circle radius is set to be at 12.5% of the screen width/height depending
          * on which of the two is larger.
          *
          * The initial position of elements on the x axis is at 10% of the same base
          * size leading to a 2.5% margin
          *
          */

        public static Vector2 bottomRightVectorPosXY(int rowNumber)
        {
            sideSize = Math.Max((int)(screenWidth * borderFactor), (int)(screenHeight * borderFactor));
            int yPos = (int)((double)screenHeight - sideSize + (int)(sideSize * borderFactor * 1.25)) + (int)(rowNumber * (double)sideSize * 0.2);
            int xPos = (int)(screenWidth * (1 - borderFactor * 0.9));
            return new Vector2(xPos, yPos);
        }
        public static int bottomRightXValue()
        {
            int xPos = (int)(screenWidth * (1 - borderFactor * 0.9));
            return xPos;

        }

        public static int bottomRightYValue(int rowNumber)
        {
            sideSize = Math.Max((int)(screenWidth * borderFactor), (int)(screenHeight * borderFactor));
            int yPos = (int)((double)screenHeight - sideSize + (int)(sideSize * borderFactor * 1.25)) + (int)(rowNumber * (double)sideSize * 0.2);
            return yPos;
        }

        public static int circleWidth()
        {
            sideSize = Math.Max((int)(screenWidth * borderFactor * 1.25), (int)(screenHeight * borderFactor * 1.25));
            return sideSize;
        }


    }
}
