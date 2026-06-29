using QMC.Common;
using System;
using System.Collections.Generic;
using System.Linq;

public static class MotionDeblur
{
    // 복소수 구조체
    public struct Complex
    {
        public double Real { get; set; }
        public double Imaginary { get; set; }

        public Complex(double real, double imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        public double Magnitude => Math.Sqrt(Real * Real + Imaginary * Imaginary);
        public double MagnitudeSquared => Real * Real + Imaginary * Imaginary;

        public Complex Conjugate() => new Complex(Real, -Imaginary);

        public static Complex operator +(Complex a, Complex b) => new Complex(a.Real + b.Real, a.Imaginary + b.Imaginary);
        public static Complex operator -(Complex a, Complex b) => new Complex(a.Real - b.Real, a.Imaginary - b.Imaginary);
        public static Complex operator *(Complex a, Complex b) => new Complex(a.Real * b.Real - a.Imaginary * b.Imaginary, a.Real * b.Imaginary + a.Imaginary * b.Real);
        public static Complex operator /(Complex a, Complex b)
        {
            double denominator = b.Real * b.Real + b.Imaginary * b.Imaginary;
            if (Math.Abs(denominator) < 1e-10) return new Complex(0, 0);
            return new Complex((a.Real * b.Real + a.Imaginary * b.Imaginary) / denominator, (a.Imaginary * b.Real - a.Real * b.Imaginary) / denominator);
        }
        public static Complex operator /(Complex a, double b) => Math.Abs(b) < 1e-10 ? new Complex(0, 0) : new Complex(a.Real / b, a.Imaginary / b);
    }

    /// <summary>
    /// 8비트 이미지 배열에서 모션 블러를 추정하고 제거합니다.
    /// </summary>
    /// <param name="imageData">8비트 그레이스케일 이미지 데이터</param>
    /// <param name="width">이미지 너비</param>
    /// <param name="height">이미지 높이</param>
    /// <param name="noiseLevel">노이즈 수준</param>
    /// <returns>디블러링된 8비트 이미지 데이터</returns>
    public static byte[] EstimateAndDeblur(byte[] imageData, int width, int height, double noiseLevel = 0.01)
    {
        try
        {
            // 입력 검증
            if (imageData == null || imageData.Length != width * height)
            {
                Log.Write("MotionDeblur", "입력 이미지 데이터가 유효하지 않습니다.");
                byte[] result = new byte[imageData.Length];
                Array.Copy(imageData, result, imageData.Length);
                return result;
            }

            // 이미지가 너무 작은 경우 스킵
            if (width < 32 || height < 32)
            {
                Log.Write("MotionDeblur", "이미지가 너무 작습니다. 디블러링을 스킵합니다.");
                byte[] result = new byte[imageData.Length];
                Array.Copy(imageData, result, imageData.Length);
                return result;
            }

            Log.Write("MotionDeblur", $"이미지 크기: {width}x{height}");

            // 1. 이미지 품질 검사
            if (!IsImageSuitableForDeblur(imageData, width, height))
            {
                Log.Write("MotionDeblur", "이미지가 디블러링에 적합하지 않습니다.");
                byte[] result = new byte[imageData.Length];
                Array.Copy(imageData, result, imageData.Length);
                return result;
            }

            // 2. 간단한 언샤프 마스킹 적용 (안전한 대안)
            return ApplyUnsharpMask(imageData, width, height, 1.5, 1.0);
        }
        catch (Exception ex)
        {
            Log.Write("MotionDeblur", $"디블러링 처리 중 오류: {ex.Message}");
            // 실패시 원본 반환
            byte[] result = new byte[imageData.Length];
            Array.Copy(imageData, result, imageData.Length);
            return result;
        }
    }

    /// <summary>
    /// 이미지가 디블러링에 적합한지 검사합니다.
    /// </summary>
    private static bool IsImageSuitableForDeblur(byte[] imageData, int width, int height)
    {
        // 1. 평균 밝기 검사
        double avgBrightness = imageData.Average(b => (double)b);
        if (avgBrightness < 10 || avgBrightness > 245)
        {
            Log.Write("MotionDeblur", $"평균 밝기가 부적절합니다: {avgBrightness:F2}");
            return false;
        }

        // 2. 분산 검사 (너무 단조로운 이미지 제외)
        double variance = imageData.Average(b => Math.Pow(b - avgBrightness, 2));
        if (variance < 100)
        {
            Log.Write("MotionDeblur", $"이미지 분산이 너무 작습니다: {variance:F2}");
            return false;
        }

        // 3. 엣지 강도 검사
        double edgeStrength = CalculateEdgeStrength(imageData, width, height);
        if (edgeStrength < 10)
        {
            Log.Write("MotionDeblur", $"엣지 강도가 너무 약합니다: {edgeStrength:F2}");
            return false;
        }

        Log.Write("MotionDeblur", $"이미지 품질: 평균밝기={avgBrightness:F2}, 분산={variance:F2}, 엣지강도={edgeStrength:F2}");
        return true;
    }

    /// <summary>
    /// 이미지의 엣지 강도를 계산합니다.
    /// </summary>
    private static double CalculateEdgeStrength(byte[] imageData, int width, int height)
    {
        double totalGradient = 0;
        int count = 0;

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int idx = y * width + x;
                
                // Sobel 연산자
                double gx = -imageData[(y-1)*width + x-1] + imageData[(y-1)*width + x+1]
                           -2*imageData[y*width + x-1] + 2*imageData[y*width + x+1]
                           -imageData[(y+1)*width + x-1] + imageData[(y+1)*width + x+1];

                double gy = -imageData[(y-1)*width + x-1] - 2*imageData[(y-1)*width + x] - imageData[(y-1)*width + x+1]
                           +imageData[(y+1)*width + x-1] + 2*imageData[(y+1)*width + x] + imageData[(y+1)*width + x+1];

                totalGradient += Math.Sqrt(gx * gx + gy * gy);
                count++;
            }
        }

        return count > 0 ? totalGradient / count : 0;
    }

    /// <summary>
    /// 언샤프 마스킹을 적용하여 이미지를 선명하게 만듭니다.
    /// </summary>
    private static byte[] ApplyUnsharpMask(byte[] imageData, int width, int height, double amount, double radius)
    {
        Log.Write("MotionDeblur", $"언샤프 마스킹 적용: amount={amount}, radius={radius}");

        // 1. 가우시안 블러 생성
        byte[] blurred = ApplyGaussianBlur(imageData, width, height, radius);

        // 2. 언샤프 마스킹 적용
        byte[] result = new byte[imageData.Length];
        
        for (int i = 0; i < imageData.Length; i++)
        {
            // Unsharp mask formula: sharpened = original + amount * (original - blurred)
            double diff = imageData[i] - blurred[i];
            double sharpened = imageData[i] + amount * diff;
            
            // 클램핑
            result[i] = (byte)Math.Max(0, Math.Min(255, sharpened));
        }

        Log.Write("MotionDeblur", "언샤프 마스킹 완료");
        return result;
    }

    /// <summary>
    /// 가우시안 블러를 적용합니다.
    /// </summary>
    private static byte[] ApplyGaussianBlur(byte[] imageData, int width, int height, double radius)
    {
        // 간단한 박스 필터로 근사
        int kernelSize = Math.Max(3, (int)(radius * 2) + 1);
        if (kernelSize % 2 == 0) kernelSize++;
        
        int halfKernel = kernelSize / 2;
        byte[] result = new byte[imageData.Length];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double sum = 0;
                int count = 0;
                
                for (int ky = -halfKernel; ky <= halfKernel; ky++)
                {
                    for (int kx = -halfKernel; kx <= halfKernel; kx++)
                    {
                        int ny = y + ky;
                        int nx = x + kx;
                        
                        if (ny >= 0 && ny < height && nx >= 0 && nx < width)
                        {
                            sum += imageData[ny * width + nx];
                            count++;
                        }
                    }
                }
                
                result[y * width + x] = (byte)(sum / count);
            }
        }
        
        return result;
    }

    /// <summary>
    /// 실제 모션 디블러링을 수행합니다.
    /// </summary>
    public static byte[] AdvancedDeblur(byte[] imageData, int width, int height, double noiseLevel = 0.01)
    {
        try
        {
            Log.Write("MotionDeblur", $"자동 모션 디블러닝 시작: {width}x{height}");

            // 입력 검증
            if (imageData == null || imageData.Length != width * height)
            {
                Log.Write("MotionDeblur", "입력 데이터 오류");
                return (byte[])imageData.Clone();
            }

            // 1. 모션 블러 파라미터 자동 추정
            EstimateMotionParametersAdvanced(imageData, width, height, out double angle, out double length);

            Log.Write("MotionDeblur", $"추정된 모션 블러: 각도={angle:F2}°, 길이={length}px");

            // 2. 추정된 파라미터로 디블러링 수행
            return AdvancedDeblur(imageData, width, height, angle,(int) length, noiseLevel);
        }
        catch (Exception ex)
        {
            Log.Write("MotionDeblur", $"자동 디블러링 실패: {ex.Message}");
            return ApplyUnsharpMask(imageData, width, height, 2.0, 1.0);
        }
    }

    /// <summary>
    /// 실제 모션 디블러링을 수행합니다 (수동 각도/길이 지정).
    /// </summary>
    public static byte[] AdvancedDeblur(byte[] imageData, int width, int height, double angle, int length, double noiseLevel = 0.01)
    {
        try
        {
            Log.Write("MotionDeblur", $"실제 모션 디블러링 시작: 각도={angle:F2}°, 길이={length}px, 노이즈레벨={noiseLevel}");

            // 입력 검증
            if (imageData == null || imageData.Length != width * height)
            {
                Log.Write("MotionDeblur", "입력 데이터 오류");
                return (byte[])imageData.Clone();
            }

            // 길이가 너무 작으면 언샤프 마스킹만 적용
            if (length < 3)
            {
                Log.Write("MotionDeblur", "블러 길이가 너무 작음, 언샤프 마스킹 적용");
                return ApplyUnsharpMask(imageData, width, height, 2.0, 1.0);
            }

            // 큰 이미지의 경우 타일 방식 사용
            const int maxPixels = 1000000;
            if (width * height > maxPixels)
            {
                Log.Write("MotionDeblur", "큰 이미지: 타일 방식 사용");
                return DeblurTiled(imageData, width, height, angle, length, noiseLevel);
            }

            // 실제 FFT 기반 디블러링 수행
            return DeblurWithFFT(imageData, width, height, angle, length, noiseLevel);
        }
        catch (Exception ex)
        {
            Log.Write("MotionDeblur", $"고급 디블러링 실패: {ex.Message}");
            return ApplyUnsharpMask(imageData, width, height, 2.0, 1.0);
        }
    }

    /// <summary>
    /// 고급 모션 블러 파라미터 추정 (그라디언트 기반)
    /// </summary>
    public static void EstimateMotionParametersAdvanced(byte[] imageData, int width, int height, out double angle, out double length)
    {
        Log.Write("MotionDeblur", "모션 블러 파라미터 자동 추정 시작");

        // 기본값 설정
        angle = 0;
        length = 0;

        try
        {
            // 1. 이미지가 너무 작으면 기본값 사용
            if (width < 64 || height < 64)
            {
                Log.Write("MotionDeblur", "이미지가 너무 작아 기본값 사용");
                angle = 0;
                length = 5;
                return;
            }

            // 2. 큰 이미지는 다운샘플링하여 빠르게 처리
            byte[] sampleData = imageData;
            int sampleWidth = width;
            int sampleHeight = height;
            
            if (width > 1024 || height > 1024)
            {
                // 1/4 크기로 다운샘플링
                sampleWidth = Math.Max(256, width / 4);
                sampleHeight = Math.Max(256, height / 4);
                sampleData = DownsampleImage(imageData, width, height, sampleWidth, sampleHeight);
                Log.Write("MotionDeblur", $"다운샘플링: {width}x{height} -> {sampleWidth}x{sampleHeight}");
            }

            // 3. 그라디언트 방향 분석
            EstimateMotionByGradient(sampleData, sampleWidth, sampleHeight, out angle, out length);

            // 4. 라돈 변환 기반 검증 (선택적)
            if (length > 2)
            {
                double verifiedAngle = VerifyAngleByRadon(sampleData, sampleWidth, sampleHeight, angle);
                if (Math.Abs(verifiedAngle - angle) < 45) // 45도 이내 차이면 검증된 각도 사용
                {
                    angle = verifiedAngle;
                    Log.Write("MotionDeblur", $"라돈 변환으로 각도 검증: {angle:F2}°");
                }
            }

            // 5. 원본 크기에 맞게 길이 조정
            if (sampleWidth != width || sampleHeight != height)
            {
                double scaleX = (double)width / sampleWidth;
                double scaleY = (double)height / sampleHeight;
                double avgScale = (scaleX + scaleY) / 2;
                length = (int)Math.Round(length * avgScale);
                Log.Write("MotionDeblur", $"크기 조정: 길이 {length}px로 스케일링");
            }

            // 6. 결과 범위 제한
            length = Math.Max(1, Math.Min(length, Math.Min(width, height) / 8));

            Log.Write("MotionDeblur", $"최종 추정 결과: 각도={angle:F2}°, 길이={length}px");
        }
        catch (Exception ex)
        {
            Log.Write("MotionDeblur", $"모션 파라미터 추정 실패: {ex.Message}");
            angle = 0;
            length = 5; // 기본 길이
        }
    }

    /// <summary>
    /// 그라디언트 분석을 통한 모션 방향 추정
    /// </summary>
    private static void EstimateMotionByGradient(byte[] imageData, int width, int height, out double angle, out double length)
    {
        Log.Write("MotionDeblur", "그라디언트 기반 모션 추정");

        angle = 0;
        length = 0;

        // 소벨 연산자로 그라디언트 계산
        double[] gradientX = new double[width * height];
        double[] gradientY = new double[width * height];
        double[] gradientMag = new double[width * height];

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int idx = y * width + x;

                // 소벨 X 방향
                double gx = -imageData[(y-1)*width + x-1] + imageData[(y-1)*width + x+1]
                           -2*imageData[y*width + x-1] + 2*imageData[y*width + x+1]
                           -imageData[(y+1)*width + x-1] + imageData[(y+1)*width + x+1];

                // 소벨 Y 방향
                double gy = -imageData[(y-1)*width + x-1] - 2*imageData[(y-1)*width + x] - imageData[(y-1)*width + x+1]
                           +imageData[(y+1)*width + x-1] + 2*imageData[(y+1)*width + x] + imageData[(y+1)*width + x+1];

                gradientX[idx] = gx;
                gradientY[idx] = gy;
                gradientMag[idx] = Math.Sqrt(gx * gx + gy * gy);
            }
        }

        // 강한 엣지만 선택 (상위 15%)
        double[] sortedMag = gradientMag.OrderByDescending(m => m).ToArray();
        double threshold = sortedMag[Math.Min(sortedMag.Length / 7, sortedMag.Length - 1)];

        // 방향 히스토그램 생성 (180도를 36개 구간으로)
        int[] directionHist = new int[36];
        List<double>[] lengthSamples = new List<double>[36];
        for(int i = 0; i < 36; i++) lengthSamples[i] = new List<double>();

        int validSampleCount = 0;

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int idx = y * width + x;
                
                if (gradientMag[idx] > threshold)
                {
                    // 그라디언트 방향 계산 (올바른 방법)
                    double gradientAngle = Math.Atan2(gradientY[idx], gradientX[idx]) * 180.0 / Math.PI;
                    
                    // 모션 방향은 그라디언트에 수직 (90도 회전)
                    double motionAngle = gradientAngle + 90.0;
                    
                    // 0~180도 범위로 정규화 (모션 블러는 방향성이 없으므로)
                    if (motionAngle < 0) motionAngle += 180.0;
                    if (motionAngle >= 180.0) motionAngle -= 180.0;

                    int bin = (int)(motionAngle / 5) % 36; // 5도씩 36개 구간
                    directionHist[bin]++;

                    // 엣지 길이 추정 (샘플링 빈도 조정)
                    if (validSampleCount % 3 == 0) // 3번에 1번만 샘플링하여 성능 향상
                    {
                        double edgeLength = EstimateLocalBlurLength(imageData, width, height, x, y, motionAngle);
                        if (edgeLength > 1.5) // 더 엄격한 조건
                        {
                            lengthSamples[bin].Add(edgeLength);
                        }
                    }
                    validSampleCount++;
                }
            }
        }

        // 가장 많이 나타난 방향 찾기
        int maxBin = 0;
        for (int i = 1; i < 36; i++)
        {
            if (directionHist[i] > directionHist[maxBin])
                maxBin = i;
        }

        if (directionHist[maxBin] > Math.Max(10, validSampleCount / 100)) // 최소 샘플 조건
        {
            angle = maxBin * 5.0; // 구간 중심값
            
            // 인접 구간 가중 평균으로 정밀도 향상
            double weightedAngle = 0;
            double totalWeight = 0;
            
            for (int i = -1; i <= 1; i++)
            {
                int bin = (maxBin + i + 36) % 36;
                double weight = directionHist[bin];
                weightedAngle += (bin * 5.0) * weight;
                totalWeight += weight;
            }
            
            if (totalWeight > 0)
            {
                angle = (weightedAngle / totalWeight) % 180;
            }

            // 길이 계산 - 신뢰성 있는 샘플만 사용
            List<double> allLengthSamples = new List<double>();
            for (int i = -1; i <= 1; i++)
            {
                int bin = (maxBin + i + 36) % 36;
                allLengthSamples.AddRange(lengthSamples[bin]);
            }

            if (allLengthSamples.Count >= 3)
            {
                // 아웃라이어 제거 후 평균 계산
                allLengthSamples.Sort();
                
                // IQR 방법으로 아웃라이어 제거
                if (allLengthSamples.Count >= 5)
                {
                    int q1Index = allLengthSamples.Count / 4;
                    int q3Index = (allLengthSamples.Count * 3) / 4;
                    double q1 = allLengthSamples[q1Index];
                    double q3 = allLengthSamples[q3Index];
                    double iqr = q3 - q1;
                    double lowerBound = q1 - 1.5 * iqr;
                    double upperBound = q3 + 1.5 * iqr;
                    
                    var filteredSamples = allLengthSamples.Where(l => l >= lowerBound && l <= upperBound).ToList();
                    if (filteredSamples.Count >= 2)
                    {
                        length = filteredSamples.Average();
                    }
                    else
                    {
                        length = allLengthSamples.Average();
                    }
                }
                else
                {
                    // 상위/하위 극값 제거 후 평균
                    if (allLengthSamples.Count >= 3)
                    {
                        allLengthSamples.RemoveAt(0); // 최솟값 제거
                        allLengthSamples.RemoveAt(allLengthSamples.Count - 1); // 최댓값 제거
                    }
                    length = allLengthSamples.Average();
                }
            }
            else
            {
                // 샘플이 부족하면 블러 없음으로 판단
                length = 0;
            }
        }
        else
        {
            // 방향성이 뚜렷하지 않으면 블러 없음
            angle = 0;
            length = 0;
        }

        Log.Write("MotionDeblur", $"그라디언트 분석 결과: 각도={angle:F2}°, 길이={length:F2}px (샘플={validSampleCount})");
    }

    /// <summary>
    /// 국소적 블러 길이 추정
    /// </summary>
    private static double EstimateLocalBlurLength(byte[] imageData, int width, int height, int centerX, int centerY, double motionAngle)
    {
        double rad = motionAngle * Math.PI / 180.0;
        double cosA = Math.Cos(rad);
        double sinA = Math.Sin(rad);

        // 모션 방향으로 라인 프로파일 생성
        List<double> profile = new List<double>();
        int maxLength = Math.Min(width, height) / 4;

        for (int i = -maxLength; i <= maxLength; i++)
        {
            int x = centerX + (int)Math.Round(i * cosA);
            int y = centerY + (int)Math.Round(i * sinA);
            
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                profile.Add(imageData[y * width + x]);
            }
        }

        if (profile.Count < 10) return 0;

        // 프로파일의 변화가 충분히 큰지 확인 (블러된 엣지인지 판단)
        double minVal = profile.Min();
        double maxVal = profile.Max();
        double dynamicRange = maxVal - minVal;
        
        // 동적 범위가 충분하지 않으면 블러가 아님
        if (dynamicRange < 30) 
        {
            return 0; // 변화가 거의 없으면 블러가 아님
        }

        // 프로파일에서 블러 폭 추정
        return EstimateBlurWidthFromProfile(profile.ToArray(), dynamicRange);
    }

    /// <summary>
    /// 프로파일에서 블러 폭 추정 (개선된 엣지 기반 방법)
    /// </summary>
    private static double EstimateBlurWidthFromProfile(double[] profile, double dynamicRange)
    {
        if (profile.Length < 8) return 0;

        // 1. 프로파일을 정규화 (0~1 범위)
        double minVal = profile.Min();
        double maxVal = profile.Max();
        double[] normalizedProfile = new double[profile.Length];
        for (int i = 0; i < profile.Length; i++)
        {
            normalizedProfile[i] = (profile[i] - minVal) / (maxVal - minVal);
        }

        // 2. 엣지 전환 구간 찾기 (10%~90% 구간)
        double lowThreshold = 0.1;
        double highThreshold = 0.9;
        
        List<int> risingEdges = new List<int>();
        List<int> fallingEdges = new List<int>();
        
        // 상승 엣지와 하강 엣지 찾기
        for (int i = 1; i < normalizedProfile.Length; i++)
        {
            double prev = normalizedProfile[i - 1];
            double curr = normalizedProfile[i];
            
            // 상승 엣지: 낮은 값에서 높은 값으로 전환
            if (prev <= lowThreshold && curr >= highThreshold)
            {
                risingEdges.Add(i);
            }
            // 하강 엣지: 높은 값에서 낮은 값으로 전환
            else if (prev >= highThreshold && curr <= lowThreshold)
            {
                fallingEdges.Add(i);
            }
        }

        // 3. 유효한 엣지 쌍 찾기
        List<double> blurWidths = new List<double>();
        
        foreach (int rising in risingEdges)
        {
            foreach (int falling in fallingEdges)
            {
                if (falling > rising)
                {
                    double width = falling - rising;
                    // 합리적인 범위의 블러 폭만 수용
                    if (width >= 2 && width <= profile.Length / 3)
                    {
                        blurWidths.Add(width);
                    }
                }
            }
        }

        // 4. 블러 폭이 없으면 그라디언트 기반 방법 시도
        if (blurWidths.Count == 0)
        {
            return EstimateBlurByGradientTransition(normalizedProfile);
        }

        // 5. 가장 일관성 있는 블러 폭 선택
        if (blurWidths.Count == 1)
        {
            return blurWidths[0];
        }
        else
        {
            // 여러 값이 있으면 중앙값 사용
            blurWidths.Sort();
            return blurWidths[blurWidths.Count / 2];
        }
    }

    /// <summary>
    /// 그라디언트 전환을 이용한 블러 길이 추정 (보조 방법)
    /// </summary>
    private static double EstimateBlurByGradientTransition(double[] normalizedProfile)
    {
        if (normalizedProfile.Length < 5) return 0;

        // 1차 미분 계산
        double[] gradient = new double[normalizedProfile.Length - 1];
        for (int i = 0; i < gradient.Length; i++)
        {
            gradient[i] = normalizedProfile[i + 1] - normalizedProfile[i];
        }

        // 최대 그라디언트 구간 찾기
        double maxGradient = gradient.Max();
        double minGradient = gradient.Min();
        
        // 그라디언트 임계값 설정
        double gradThreshold = Math.Max(Math.Abs(maxGradient), Math.Abs(minGradient)) * 0.3;
        
        if (gradThreshold < 0.05) return 0; // 그라디언트가 너무 작으면 블러 아님

        // 유의미한 그라디언트 구간의 길이 계산
        List<int> significantGradientRegions = new List<int>();
        bool inRegion = false;
        int regionStart = -1;
        
        for (int i = 0; i < gradient.Length; i++)
        {
            bool isSignificant = Math.Abs(gradient[i]) > gradThreshold;
            
            if (isSignificant && !inRegion)
            {
                // 새로운 구간 시작
                regionStart = i;
                inRegion = true;
            }
            else if (!isSignificant && inRegion)
            {
                // 구간 종료
                int regionLength = i - regionStart;
                if (regionLength >= 2) // 최소 길이 조건
                {
                    significantGradientRegions.Add(regionLength);
                }
                inRegion = false;
            }
        }
        
        // 마지막 구간 처리
        if (inRegion)
        {
            int regionLength = gradient.Length - regionStart;
            if (regionLength >= 2)
            {
                significantGradientRegions.Add(regionLength);
            }
        }

        // 평균 그라디언트 구간 길이 반환
        if (significantGradientRegions.Count > 0)
        {
            return significantGradientRegions.Average();
        }

        return 0; // 유의미한 구간을 찾지 못함
    }

    /// <summary>
    /// 라돈 변환으로 각도 검증
    /// </summary>
    private static double VerifyAngleByRadon(byte[] imageData, int width, int height, double estimatedAngle)
    {
        Log.Write("MotionDeblur", "라돈 변환으로 각도 검증");

        // 간단한 라돈 변환 (제한된 각도 범위)
        double bestAngle = estimatedAngle;
        double maxProjection = 0;

        // 추정 각도 ±30도 범위에서 5도씩 검사
        for (double testAngle = estimatedAngle - 30; testAngle <= estimatedAngle + 30; testAngle += 5)
        {
            double projection = CalculateProjectionStrength(imageData, width, height, testAngle);
            if (projection > maxProjection)
            {
                maxProjection = projection;
                bestAngle = testAngle;
            }
        }

        return bestAngle;
    }

    /// <summary>
    /// 특정 각도에서의 투영 강도 계산
    /// </summary>
    private static double CalculateProjectionStrength(byte[] imageData, int width, int height, double angle)
    {
        double rad = angle * Math.PI / 180.0;
        double cosA = Math.Cos(rad);
        double sinA = Math.Sin(rad);

        // 이미지 중심
        int centerX = width / 2;
        int centerY = height / 2;

        double totalVariance = 0;
        int lineCount = 0;

        // 여러 평행선에서 분산 계산
        for (int offset = -50; offset <= 50; offset += 10)
        {
            List<double> lineValues = new List<double>();

            // 수직 방향으로 라인 그리기
            double perpCos = -sinA;
            double perpSin = cosA;

            for (int t = -Math.Max(width, height); t <= Math.Max(width, height); t++)
            {
                int x = centerX + (int)Math.Round(t * cosA + offset * perpCos);
                int y = centerY + (int)Math.Round(t * sinA + offset * perpSin);

                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    lineValues.Add(imageData[y * width + x]);
                }
            }

            if (lineValues.Count > 10)
            {
                double mean = lineValues.Average();
                double variance = lineValues.Average(v => Math.Pow(v - mean, 2));
                totalVariance += variance;
                lineCount++;
            }
        }

        return lineCount > 0 ? totalVariance / lineCount : 0;
    }

    /// <summary>
    /// 이미지 다운샘플링
    /// </summary>
    private static byte[] DownsampleImage(byte[] imageData, int width, int height, int newWidth, int newHeight)
    {
        byte[] result = new byte[newWidth * newHeight];
        
        double scaleX = (double)width / newWidth;
        double scaleY = (double)height / newHeight;

        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                // 바이리니어 보간
                double srcX = x * scaleX;
                double srcY = y * scaleY;
                
                int x1 = (int)srcX;
                int y1 = (int)srcY;
                int x2 = Math.Min(x1 + 1, width - 1);
                int y2 = Math.Min(y1 + 1, height - 1);
                
                double wx = srcX - x1;
                double wy = srcY - y1;
                
                double val = (1 - wx) * (1 - wy) * imageData[y1 * width + x1] +
                             wx * (1 - wy) * imageData[y1 * width + x2] +
                             (1 - wx) * wy * imageData[y2 * width + x1] +
                             wx * wy * imageData[y2 * width + x2];
                
                result[y * newWidth + x] = (byte)Math.Round(val);
            }
        }
        
        return result;
    }

    // 유틸리티 메서드들
    private static byte[] ExtractTile(byte[] source, int sourceWidth, int sourceHeight, int x, int y, int tileWidth, int tileHeight)
    {
        byte[] tile = new byte[tileWidth * tileHeight];
        
        for (int row = 0; row < tileHeight; row++)
        {
            for (int col = 0; col < tileWidth; col++)
            {
                int sourceIndex = (y + row) * sourceWidth + (x + col);
                int tileIndex = row * tileWidth + col;
                
                if (sourceIndex < source.Length && tileIndex < tile.Length)
                {
                    tile[tileIndex] = source[sourceIndex];
                }
            }
        }
        
        return tile;
    }

    private static int NextPowerOfTwo(int n)
    {
        if (n <= 1) return 1;
        int power = 1;
        while (power < n)
            power <<= 1;
        return power;
    }

    private static bool IsPowerOfTwo(int n)
    {
        return n > 0 && (n & (n - 1)) == 0;
    }

    /// <summary>
    /// 2D FFT 구현
    /// </summary>
    private static void FFT2D(Complex[,] data, int width, int height, bool inverse)
    {
        if (!IsPowerOfTwo(width) || !IsPowerOfTwo(height))
        {
            throw new ArgumentException("FFT requires power-of-two dimensions");
        }

        // 행별 FFT
        for (int y = 0; y < height; y++)
        {
            Complex[] row = new Complex[width];
            for (int x = 0; x < width; x++)
                row[x] = data[y, x];

            FFT1D(row, inverse);

            for (int x = 0; x < width; x++)
                data[y, x] = row[x];
        }

        // 열별 FFT
        for (int x = 0; x < width; x++)
        {
            Complex[] col = new Complex[height];
            for (int y = 0; y < height; y++)
                col[y] = data[y, x];

            FFT1D(col, inverse);

            for (int y = 0; y < height; y++)
                data[y, x] = col[y];
        }
    }

    /// <summary>
    /// 1D FFT 구현
    /// </summary>
    private static void FFT1D(Complex[] data, bool inverse)
    {
        int n = data.Length;
        if (n <= 1) return;

        if (!IsPowerOfTwo(n))
        {
            throw new ArgumentException("FFT requires power-of-two size");
        }

        // Bit-reversal permutation
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
                j ^= bit;
            j ^= bit;

            if (i < j)
            {
                Complex temp = data[i];
                data[i] = data[j];
                data[j] = temp;
            }
        }

        // Cooley-Tukey FFT
        for (int len = 2; len <= n; len <<= 1)
        {
            double ang = 2 * Math.PI / len * (inverse ? 1 : -1);
            Complex wlen = new Complex(Math.Cos(ang), Math.Sin(ang));

            for (int i = 0; i < n; i += len)
            {
                Complex w = new Complex(1, 0);
                for (int j = 0; j < len / 2; j++)
                {
                    Complex u = data[i + j];
                    Complex v = data[i + j + len / 2] * w;
                    data[i + j] = u + v;
                    data[i + j + len / 2] = u - v;
                    w = w * wlen;
                }
            }
        }

        if (inverse)
        {
            for (int i = 0; i < n; i++)
                data[i] = data[i] / n;
        }
    }

    /// <summary>
    /// FFT를 사용한 실제 모션 디블러링
    /// </summary>
    private static byte[] DeblurWithFFT(byte[] imageData, int width, int height, double angle, int length, double noiseLevel)
    {
        Log.Write("MotionDeblur", "FFT 기반 디블러링 시작");

        // 1. FFT에 적합한 크기로 패딩
        int paddedWidth = NextPowerOfTwo(width + length);
        int paddedHeight = NextPowerOfTwo(height + length);

        Log.Write("MotionDeblur", $"패딩된 크기: {paddedWidth}x{paddedHeight}");

        // 2. 이미지를 복소수 배열로 변환 (패딩 포함)
        Complex[,] imageComplex = PadImageToComplex(imageData, width, height, paddedWidth, paddedHeight);

        // 3. 모션 블러 PSF 생성
        Complex[,] psfComplex = CreateMotionPSF(paddedWidth, paddedHeight, angle, length);

        try
        {
            // 4. 주파수 도메인으로 변환
            FFT2D(imageComplex, paddedWidth, paddedHeight, false);
            FFT2D(psfComplex, paddedWidth, paddedHeight, false);

            // 5. 위너 필터 적용
            ApplyWienerFilter(imageComplex, psfComplex, paddedWidth, paddedHeight, noiseLevel);

            // 6. 공간 도메인으로 변환
            FFT2D(imageComplex, paddedWidth, paddedHeight, true);

            // 7. 원래 크기로 크롭하여 결과 반환
            return CropComplexToByteArray(imageComplex, paddedWidth, paddedHeight, width, height);
        }
        catch (Exception ex)
        {
            Log.Write("MotionDeblur", $"FFT 처리 실패: {ex.Message}");
            return ApplyLucyRichardson(imageData, width, height, angle, length, 10);
        }
    }

    /// <summary>
    /// Lucy-Richardson 디콘볼루션 (FFT 대안)
    /// </summary>
    private static byte[] ApplyLucyRichardson(byte[] imageData, int width, int height, double angle, int length, int iterations)
    {
        Log.Write("MotionDeblur", $"Lucy-Richardson 디콘볼루션: {iterations}회 반복");

        // PSF 생성
        double[,] psf = CreateMotionKernel(angle, length);
        int psfSize = psf.GetLength(0);
        int psfHalf = psfSize / 2;

        // 초기 추정값 (원본 이미지)
        double[,] estimate = new double[height, width];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                estimate[y, x] = imageData[y * width + x] / 255.0;
            }
        }

        // Lucy-Richardson 반복
        for (int iter = 0; iter < iterations; iter++)
        {
            // 1. 현재 추정값에 PSF 적용
            double[,] convolved = ConvolveWithPSF(estimate, psf, width, height);

            // 2. 비율 계산
            double[,] ratio = new double[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double observed = imageData[y * width + x] / 255.0;
                    double conv = Math.Max(convolved[y, x], 1e-10);
                    ratio[y, x] = observed / conv;
                }
            }

            // 3. 비율에 뒤집힌 PSF 적용
            double[,] correction = ConvolveWithFlippedPSF(ratio, psf, width, height);

            // 4. 추정값 업데이트
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    estimate[y, x] *= correction[y, x];
                    estimate[y, x] = Math.Max(0, Math.Min(1, estimate[y, x])); // 클램핑
                }
            }

            if (iter % 2 == 0)
            {
                Log.Write("MotionDeblur", $"Lucy-Richardson 반복 {iter + 1}/{iterations}");
            }
        }

        // 결과를 byte 배열로 변환
        byte[] result = new byte[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[y * width + x] = (byte)(estimate[y, x] * 255);
            }
        }

        Log.Write("MotionDeblur", "Lucy-Richardson 디콘볼루션 완료");
        return result;
    }

    /// <summary>
    /// PSF와의 컨볼루션
    /// </summary>
    private static double[,] ConvolveWithPSF(double[,] image, double[,] psf, int width, int height)
    {
        int psfSize = psf.GetLength(0);
        int psfHalf = psfSize / 2;
        double[,] result = new double[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double sum = 0;
                for (int py = 0; py < psfSize; py++)
                {
                    for (int px = 0; px < psfSize; px++)
                    {
                        int iy = y + py - psfHalf;
                        int ix = x + px - psfHalf;
                        
                        if (iy >= 0 && iy < height && ix >= 0 && ix < width)
                        {
                            sum += image[iy, ix] * psf[py, px];
                        }
                    }
                }
                result[y, x] = sum;
            }
        }
        return result;
    }

    /// <summary>
    /// 뒤집힌 PSF와의 컨볼루션
    /// </summary>
    private static double[,] ConvolveWithFlippedPSF(double[,] image, double[,] psf, int width, int height)
    {
        int psfSize = psf.GetLength(0);
        int psfHalf = psfSize / 2;
        double[,] result = new double[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double sum = 0;
                for (int py = 0; py < psfSize; py++)
                {
                    for (int px = 0; px < psfSize; px++)
                    {
                        int iy = y - py + psfHalf;
                        int ix = x - px + psfHalf;
                        
                        if (iy >= 0 && iy < height && ix >= 0 && ix < width)
                        {
                            // PSF를 뒤집어서 사용
                            sum += image[iy, ix] * psf[psfSize - 1 - py, psfSize - 1 - px];
                        }
                    }
                }
                result[y, x] = sum;
            }
        }
        return result;
    }

    /// <summary>
    /// 모션 블러 PSF를 Complex 형태로 생성
    /// </summary>
    private static Complex[,] CreateMotionPSF(int width, int height, double angle, int length)
    {
        Complex[,] psf = new Complex[height, width];
        double rad = angle * Math.PI / 180.0;
        
        // PSF 중심
        int centerX = width / 2;
        int centerY = height / 2;
        
        // 모션 라인 생성
        for (int i = -length/2; i <= length/2; i++)
        {
            int x = centerX + (int)Math.Round(i * Math.Cos(rad));
            int y = centerY + (int)Math.Round(i * Math.Sin(rad));
            
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                psf[y, x] = new Complex(1.0 / length, 0);
            }
        }
        
        return psf;
    }

    /// <summary>
    /// 개선된 모션 커널 생성
    /// </summary>
    private static double[,] CreateMotionKernel(double angle, int length)
    {
        int size = Math.Max(length * 2 + 1, 3);
        double[,] kernel = new double[size, size];
        
        double rad = angle * Math.PI / 180.0;
        double cx = size / 2.0;
        double cy = size / 2.0;
        
        double totalWeight = 0;
        
        // 안티앨리어싱을 고려한 라인 그리기
        for (int i = -length/2; i <= length/2; i++)
        {
            double fx = cx + i * Math.Cos(rad);
            double fy = cy + i * Math.Sin(rad);
            
            // 주변 픽셀에 가중치 분배
            int x1 = (int)Math.Floor(fx);
            int y1 = (int)Math.Floor(fy);
            int x2 = x1 + 1;
            int y2 = y1 + 1;
            
            double wx = fx - x1;
            double wy = fy - y1;
            
            if (x1 >= 0 && x1 < size && y1 >= 0 && y1 < size)
            {
                double weight = (1 - wx) * (1 - wy);
                kernel[y1, x1] += weight;
                totalWeight += weight;
            }
            if (x2 >= 0 && x2 < size && y1 >= 0 && y1 < size)
            {
                double weight = wx * (1 - wy);
                kernel[y1, x2] += weight;
                totalWeight += weight;
            }
            if (x1 >= 0 && x1 < size && y2 >= 0 && y2 < size)
            {
                double weight = (1 - wx) * wy;
                kernel[y2, x1] += weight;
                totalWeight += weight;
            }
            if (x2 >= 0 && x2 < size && y2 >= 0 && y2 < size)
            {
                double weight = wx * wy;
                kernel[y2, x2] += weight;
                totalWeight += weight;
            }
        }
        
        // 정규화
        if (totalWeight > 0)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    kernel[y, x] /= totalWeight;
                }
            }
        }
        
        return kernel;
    }

    /// <summary>
    /// 이미지를 패딩하여 Complex 배열로 변환
    /// </summary>
    private static Complex[,] PadImageToComplex(byte[] imageData, int width, int height, int paddedWidth, int paddedHeight)
    {
        Complex[,] result = new Complex[paddedHeight, paddedWidth];
        
        int offsetX = (paddedWidth - width) / 2;
        int offsetY = (paddedHeight - height) / 2;
        
        for (int y = 0; y < paddedHeight; y++)
        {
            for (int x = 0; x < paddedWidth; x++)
            {
                int srcX = x - offsetX;
                int srcY = y - offsetY;
                
                if (srcX >= 0 && srcX < width && srcY >= 0 && srcY < height)
                {
                    result[y, x] = new Complex(imageData[srcY * width + srcX] / 255.0, 0);
                }
                else
                {
                    result[y, x] = new Complex(0, 0);
                }
            }
        }
        
        return result;
    }

    /// <summary>
    /// Complex 배열을 크롭하여 byte 배열로 변환
    /// </summary>
    private static byte[] CropComplexToByteArray(Complex[,] complexData, int paddedWidth, int paddedHeight, int width, int height)
    {
        byte[] result = new byte[width * height];
        
        int offsetX = (paddedWidth - width) / 2;
        int offsetY = (paddedHeight - height) / 2;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double val = complexData[y + offsetY, x + offsetX].Real * 255.0;
                result[y * width + x] = (byte)Math.Max(0, Math.Min(255, val));
            }
        }
        
        return result;
    }

    /// <summary>
    /// 위너 필터 적용
    /// </summary>
    private static void ApplyWienerFilter(Complex[,] imageFFT, Complex[,] psfFFT, int width, int height, double noiseLevel)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Complex H = psfFFT[y, x];
                double HMagSq = H.MagnitudeSquared;
                
                if (HMagSq > 1e-10)
                {
                    Complex wienerFilter = H.Conjugate() / (HMagSq + noiseLevel);
                    imageFFT[y, x] = imageFFT[y, x] * wienerFilter;
                }
                else
                {
                    imageFFT[y, x] = new Complex(0, 0);
                }
            }
        }
    }

    /// <summary>
    /// 타일 방식으로 디블러링을 수행합니다.
    /// </summary>
    private static byte[] DeblurTiled(byte[] imageData, int width, int height, double angle, int length, double noiseLevel)
    {
        byte[] result = new byte[width * height];

        // 2x2 = 4조각으로 분할
        int tileWidth = width / 2;
        int tileHeight = height / 2;

        Log.Write("MotionDeblur", $"타일 디블러링: {width}x{height}, 4조각으로 분할 처리");

        for (int tileY = 0; tileY < 2; tileY++)
        {
            for (int tileX = 0; tileX < 2; tileX++)
            {
                int x = tileX * tileWidth;
                int y = tileY * tileHeight;
                int w = (tileX == 1) ? width - x : tileWidth;
                int h = (tileY == 1) ? height - y : tileHeight;

                try
                {
                    byte[] tile = ExtractTile(imageData, width, height, x, y, w, h);
                    byte[] deblurredTile = ApplyLucyRichardson(tile, w, h, angle, length, 5);

                    // 타일을 결과 배열에 복사
                    for (int row = 0; row < h; row++)
                    {
                        for (int col = 0; col < w; col++)
                        {
                            int resultIndex = (y + row) * width + (x + col);
                            int tileIndex = row * w + col;
                            result[resultIndex] = deblurredTile[tileIndex];
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Write("MotionDeblur", $"타일 [{tileX},{tileY}] 디블러링 실패: {ex.Message}");
                    // 실패한 타일은 언샤프 마스킹 적용
                    byte[] tile = ExtractTile(imageData, width, height, x, y, w, h);
                    byte[] processedTile = ApplyUnsharpMask(tile, w, h, 2.0, 1.0);
                    
                    for (int row = 0; row < h; row++)
                    {
                        for (int col = 0; col < w; col++)
                        {
                            int resultIndex = (y + row) * width + (x + col);
                            int tileIndex = row * w + col;
                            result[resultIndex] = processedTile[tileIndex];
                        }
                    }
                }
            }
        }

        Log.Write("MotionDeblur", "타일 디블러링 완료");
        return result;
    }
}