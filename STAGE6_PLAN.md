# Stage 6 PLAN — 추가 Cognex Backend Tools

## 목표
현재 Cognex backend 의 Pattern matching (PMAlign) + Blob 외에 산업 비전에서 흔히 쓰는 도구 추가:
- **Caliper** — 에지 검출 + 거리 측정 (다이 폭/높이 정밀 측정)
- **Histogram** — 밝기 통계 (조명 균질성 점검)
- **ColorMatch** — 컬러 패턴 매칭 (잉크/마킹 검출)

## 새 인터페이스

### IEdgeFinder (추상)
```csharp
public interface IEdgeFinder {
    string Id { get; }
    Roi MeasureRoi { get; set; }
    double EdgeThreshold { get; set; }
    EdgeMeasurement Measure(Bitmap image);
}

public class EdgeMeasurement {
    public bool Success;
    public double WidthPixels;
    public double HeightPixels;
    public double EdgeStrength;
    public List<Point> EdgePoints;
}
```

### IHistogramAnalyzer
```csharp
public interface IHistogramAnalyzer {
    string Id { get; }
    Roi AnalysisRoi { get; set; }
    HistogramResult Analyze(Bitmap image);
}

public class HistogramResult {
    public double Mean;
    public double Stdev;
    public int Min, Max;
    public double[] Distribution;
}
```

### IColorMatcher
```csharp
public interface IColorMatcher {
    string Id { get; }
    Color TargetColor { get; set; }
    int Tolerance { get; set; }
    ColorMatchResult Match(Bitmap image);
}

public class ColorMatchResult {
    public bool Success;
    public double MatchPercent;
    public int MatchedPixels;
}
```

## Cognex 백엔드 매핑
- Caliper → CogCaliperTool
- Histogram → CogHistogramTool
- ColorMatch → CogColorMatchTool

## NON-GOAL
- 라이선스 동글 검증
- 실 카메라 grab 테스트
