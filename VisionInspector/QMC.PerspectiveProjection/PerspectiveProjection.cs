
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if PP
using MathNet.Numerics.LinearAlgebra;
#endif
namespace QMC.PerspectiveProjection
{
    public class QMCMatrix
    {
		public int col;
		public int row;
		public double[,] m_dMatrix;
		const double EPS = 0.000000000000001;
		public QMCMatrix(int row,int col)
        {
			this .row = row;
			this .col = col;
			m_dMatrix = new double[row, col];
			for (int i = 0; i < row; i++)
            {	
				for (int j = 0; j < col; j++)
                {
					m_dMatrix[i,j] = 0;
				}
            }
        }

		public QMCMatrix Matrix_inv()
        {
			QMCMatrix qMCMatrix = new QMCMatrix(this.row,this.col);
			QMCMatrix qMCMatrixN = new QMCMatrix(this.row, this.col*2);
			int iter, i, j, k;

			double v;

			double tmp;
			int max_key;

			if (this.col != this.row)
				return qMCMatrix;

			iter = this.row;
			
			// copy it
			for (j = 0; j < iter; j++)
				for (i = 0; i < iter; i++)
					qMCMatrixN.m_dMatrix[j,i] = this.m_dMatrix[j,i];

			// insert identity matrix
			for (i = 0; i < iter; i++)
				qMCMatrixN.m_dMatrix[i,i + iter] = 1.0;

			// start gauss elimination
			for (i = 0; i < iter; i++)
			{

				// find max
				max_key = i;
				for (j = i + 1; j < iter; j++)
					if (qMCMatrixN.m_dMatrix[j,i] > qMCMatrixN.m_dMatrix[max_key,i])
						max_key = j;

				// swap with current row
				if (max_key != i)
				{
					for (j = 0; j < iter * 2; j++)
					{
						tmp = qMCMatrixN.m_dMatrix[i,j];
						qMCMatrixN.m_dMatrix[i,j] = qMCMatrixN.m_dMatrix[max_key,j];
						qMCMatrixN.m_dMatrix[max_key,j] = tmp;
					}
				}

				// normalize
				v = qMCMatrixN.m_dMatrix[i,i];
				for (j = i + 1; j < iter * 2; j++)
					qMCMatrixN.m_dMatrix[i,j] /= v + EPS;

				for (j = i + 1; j < iter; j++)
				{
					v = qMCMatrixN.m_dMatrix[j,i];
					qMCMatrixN.m_dMatrix[j,i] = 0.0;
					for (k = i + 1; k < iter * 2; k++)
					{
						qMCMatrixN.m_dMatrix[j,k] -= qMCMatrixN.m_dMatrix[i,k] * v;
					}
				}

			}
			for (i = iter - 2; i >= 0; i--)
			{

				for (j = i; j >= 0; j--)
				{
					v = qMCMatrixN.m_dMatrix[j,i + 1];
					for (k = 0; k < iter * 2; k++)
					{
						qMCMatrixN.m_dMatrix[j,k] -= qMCMatrixN.m_dMatrix[i + 1,k] * v;
					}
				}
			}

			// copy it
			for (j = 0; j < iter; j++)
				for (i = 0; i < iter; i++)
					qMCMatrix.m_dMatrix[j,i] = qMCMatrixN.m_dMatrix[j,i + iter];

			

			return qMCMatrix;
			
		}
		public QMCMatrix Matrix_Multi(QMCMatrix qMCTarget)
		{
			QMCMatrix qMCMatrix = new QMCMatrix(this.row, this.col);

			
			int col, row, iter;
			int i, j, k;

			if (this.col != qMCTarget.row)
				return qMCMatrix;

			row = this.row;
			col = qMCTarget.col;

			iter = this.col;

			
			for (j = 0; j < row; j++)
			{
				for (i = 0; i < col; i++)
				{
					for (k = 0; k < iter; k++)
					{
						qMCMatrix.m_dMatrix[j,i] += this.m_dMatrix[j,k] * qMCTarget.m_dMatrix[k,i];
					}
				}
			}
			


			return qMCMatrix;
		}

	}

    public class PerspectiveProjection
    {
		public float m_dXOffset = 300;
		public float m_dYOffset = 300;
		static Dictionary<String, List<List<PointF>>> m_dic = new Dictionary<String, List<List<PointF>>>();

        public static void ClearBuffer()
        {
			lock(PerspectiveProjection.m_dic)
			{
                m_dic.Clear();
			}
        }
        public QMCMatrix projection_matrix(List<PointF> ptSourceOrg, List<PointF> ptTargetOrg)
		{
			List<PointF> ptSource = new List<PointF>();
			List<PointF> ptTarget = new List<PointF>();

			for (int iter = 0; iter < ptSourceOrg.Count; iter ++)
            {
				ptSource.Add(new PointF(ptSourceOrg[iter].X + m_dXOffset, ptSourceOrg[iter].Y + m_dYOffset));
			}

			for (int iter = 0; iter < ptTargetOrg.Count; iter++)
			{
				ptTarget.Add(new PointF(ptTargetOrg[iter].X + m_dXOffset, ptTargetOrg[iter].Y + m_dYOffset));
			}

			QMCMatrix qMCMatrixA = new QMCMatrix(8,8);
			QMCMatrix qMCMatrixB = new QMCMatrix(8, 1);
			QMCMatrix qMCMatrixC = new QMCMatrix(8, 8);
			QMCMatrix qMCMatrixProjection = new QMCMatrix(3, 3);

			if(ptSource != null && ptTarget != null)
            {
				if(ptSource.Count == ptTarget.Count)
                {
					for (int iter = 0; iter < ptSource.Count; iter++)
					{
						qMCMatrixA.m_dMatrix[iter, 0] = ptSource[iter].X;//[0][0]  a->var[0][0] = x[0];
						qMCMatrixA.m_dMatrix[iter, 1] = ptSource[iter].Y;//[0][1]	a->var[0][1] = y[0];
						qMCMatrixA.m_dMatrix[iter, 2] = 1;//[0][2]  a->var[0][2] = 1.0;
						qMCMatrixA.m_dMatrix[iter, 3] = 0;//[0][3]
						qMCMatrixA.m_dMatrix[iter, 4] = 0;//[0][4]
						qMCMatrixA.m_dMatrix[iter, 5] = 0;//[0][5]
						qMCMatrixA.m_dMatrix[iter, 6] = -1 * ptTarget[iter].X * ptSource[iter].X;//[0][6]  a->var[0][6] = -1 * _x[0] * x[0];
						qMCMatrixA.m_dMatrix[iter, 7] = -1 * ptTarget[iter].X * ptSource[iter].Y;//[0][7]  a->var[0][7] = -1 * _x[0] * y[0];

					}

					for (int iter = 0; iter < ptSource.Count; iter++)
					{
						qMCMatrixA.m_dMatrix[iter + 4, 0] = 0;//[0][0]  a->var[0][0] = x[0];
						qMCMatrixA.m_dMatrix[iter + 4, 1] = 0;//[0][1]	a->var[0][1] = y[0];
						qMCMatrixA.m_dMatrix[iter + 4, 2] = 0;//[0][2]  a->var[0][2] = 1.0;
						qMCMatrixA.m_dMatrix[iter + 4, 3] = ptSource[iter].X;//[0][3]
						qMCMatrixA.m_dMatrix[iter + 4, 4] = ptSource[iter].Y;//[0][4]
						qMCMatrixA.m_dMatrix[iter + 4, 5] = 1;//[0][5]
						qMCMatrixA.m_dMatrix[iter + 4, 6] = -1 * ptSource[iter].X * ptTarget[iter].Y;//[0][6]  a->var[0][6] = -1 * _x[0] * x[0];
						qMCMatrixA.m_dMatrix[iter + 4, 7] = -1 * ptSource[iter].Y * ptTarget[iter].Y;//[0][7]  a->var[0][7] = -1 * _x[0] * y[0];

					}
					for (int iter = 0; iter < ptTarget.Count; iter++)
					{
						qMCMatrixB.m_dMatrix[iter,0] = ptTarget[iter].X;

					}
					for (int iter = 0; iter < ptTarget.Count; iter++)
					{
						qMCMatrixB.m_dMatrix[iter+4, 0] = ptTarget[iter].Y;
						
					}
					

					QMCMatrix qMCMatrix_inv;
					qMCMatrix_inv = qMCMatrixA.Matrix_inv();
					qMCMatrixC = qMCMatrix_inv.Matrix_Multi(qMCMatrixB);


					qMCMatrixProjection.m_dMatrix[0, 0] = qMCMatrixC.m_dMatrix[0,0];//[0][0]
					qMCMatrixProjection.m_dMatrix[0, 1] = qMCMatrixC.m_dMatrix[1, 0];//[0][1]
					qMCMatrixProjection.m_dMatrix[0, 2] = qMCMatrixC.m_dMatrix[2, 0];//[0][2]

					qMCMatrixProjection.m_dMatrix[1, 0] = qMCMatrixC.m_dMatrix[3, 0];//[0][0]
					qMCMatrixProjection.m_dMatrix[1, 1] = qMCMatrixC.m_dMatrix[4, 0];//[0][1]
					qMCMatrixProjection.m_dMatrix[1, 2] = qMCMatrixC.m_dMatrix[5, 0];//[0][2]
					
					qMCMatrixProjection.m_dMatrix[2, 0] = qMCMatrixC.m_dMatrix[6, 0];//[0][0]
					qMCMatrixProjection.m_dMatrix[2, 1] = qMCMatrixC.m_dMatrix[7, 0];//[0][1]
					qMCMatrixProjection.m_dMatrix[2, 2] = 1;//[0][2]

				}
            }

			
			return qMCMatrixProjection;
		}

		public PointF GetPerspectiveProjectionPoint(double dX,double dY,QMCMatrix qMCMatrix)
        {
			PointF point = new PointF();
			dX += m_dXOffset;
			dY += m_dYOffset;

			double W = qMCMatrix.m_dMatrix[2, 0] * dX + qMCMatrix.m_dMatrix[2, 1] * dY + qMCMatrix.m_dMatrix[2, 2];

			point.X = (float)((qMCMatrix.m_dMatrix[0, 0] * dX + qMCMatrix.m_dMatrix[0, 1] * dY + qMCMatrix.m_dMatrix[0, 2]) / W) - m_dXOffset;
			point.Y = (float)((qMCMatrix.m_dMatrix[1, 0] * dX + qMCMatrix.m_dMatrix[1, 1] * dY + qMCMatrix.m_dMatrix[1, 2]) / W) - m_dYOffset;

			return point;
		}

		public void bwarping(ref byte[] dst, byte[] src, QMCMatrix matrix, int width, int hight)
		{
			
			QMCMatrix matrix_inv = matrix.Matrix_inv();
			string strKey = "";
			foreach (var v in matrix.m_dMatrix)
			{
				strKey += v.ToString();
            }
            List<List<PointF>> pointFs = null;
            lock (PerspectiveProjection.m_dic)
			{   
                if (PerspectiveProjection.m_dic.ContainsKey(strKey))
                {
                    pointFs = PerspectiveProjection.m_dic[strKey];

                }
                else
                {
                    pointFs = new List<List<PointF>>();
                    {
                        for (int j = 0; j < hight; j++)
                        {
                            List<PointF> points = new List<PointF>();
                            for (int i = 0; i < width; i++)
                            {
                                PointF pt = GetPerspectiveProjectionPoint(i, j, matrix_inv);
                                points.Add(pt);
                            }
                            pointFs.Add(points);

                        }
                    }
                    PerspectiveProjection.m_dic.Add(strKey, pointFs);

                }
            }

            
			byte[] dest= dst;

            Parallel.For(0, hight, (j) =>
			{
				int nOffset = j * width;

                for (int i = 0; i < width; i++)
				{
					PointF pt = pointFs[j][i];

                    //int x, y;
					
                    int x = (int)pt.X;
					int y = (int)pt.Y;
                    int WeightX = (int)((pt.X - x) *10); 
					int WeightY = (int)((pt.Y - y) *10);
                    if (x >= width || y >= hight || x < 0 || y < 0)
					{
						dest[nOffset + i] = 255;

                        continue;
					}
					int nCenter = y * width + x;

					int nSum = src[nCenter];
                    dest[nOffset + i] = (byte)nSum;
					int nCount = 1;

					if (y > 0 && y < hight - 1)
					{
						nSum = src[nCenter] * (10 - WeightY); 
						nSum += src[nCenter + width] * WeightY;
                        nCount = 10;
					}

					if (x > 0 && x < width - 1)
					{
						nSum += src[nCenter] * (10 - WeightX);
                        nSum += src[nCenter + 1] * WeightX; 
						nCount += 10;
					}
					int value = (nSum / nCount);
					if(value < 0)
					{
						value = 0;
                    }
					if(value > 255)
					{
                        value = 255;

                    }
					dest[nOffset + i] = (byte)value;
				}
			});
        }


		public void fwarping(ref byte[] dst, byte[] src, QMCMatrix matrix,int w, int h)
		{
			int i, j, x, y;

			//double W;

			if (matrix.row != 3 || matrix.col != 3)
				return;
			for (j = 0; j < h; j++)
			{
				for (i = 0; i < w; i++)
				{
					dst[j * w + i] = 255;
				}
			}
			for (j = 0; j < h; j++)
			{
				for (i = 0; i < w; i++)
				{
					PointF pt = GetPerspectiveProjectionPoint(i, j, matrix);

					//W = matrix.m_dMatrix[2,0] * i + matrix.m_dMatrix[2,1] * j + matrix.m_dMatrix[2,2];

					//x = (int)((matrix.m_dMatrix[0,0] * i + matrix.m_dMatrix[0,1] * j + matrix.m_dMatrix[0,2]) / W);
					//y = (int)((matrix.m_dMatrix[1,0] * i + matrix.m_dMatrix[1,1] * j + matrix.m_dMatrix[1,2]) / W);
					x = (int)pt.X;
					y = (int)pt.Y;
					if (x >= w || y>=h || x <0 || y <0)
                    {
						continue;
					}
						

					dst[y * w + x] = src[(j * w) + i];
				}
			}

		}
	}
}
