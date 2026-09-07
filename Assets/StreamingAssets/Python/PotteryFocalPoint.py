from pysensationcore import *

import math

# PotteryHaptics用のSensation Block。
# 指位置を中心に、小さな円軌道を高速で描き続ける(空間時間変調・STM)ことで
# 触覚を提示する。
#
# 【2026-09 大幅修正】当初は「1点を静止させたまま、強さだけを140Hzで上下させる」
# 振幅変調(AM)方式だった。しかし教授から提供されたUltrahaptics公式サンプル
# (CircleSensation, RenderPath+CirclePathをdrawFrequency=70Hzで使用)を実機で
# 確認したところ、intensityは固定(変調なし)のまま、焦点を実際に高速で
# 動かし続ける方式(STM)の方が、圧倒的に強く感知できることが分かった。
# これを踏まえ、AMからSTMへ方式を変更した。
#
# 【座標系の注意】pointはC#側(HapticOutputController)で既にエミッタ空間に
# 変換済みの(x, y, z)。UnityToEmitterSpace.Transformの変換により、
# エミッタ空間ではz軸が「高さ」に相当する(Unity空間のY軸と対応)。
# そのため、円軌道はx・y成分(高さに垂直な面)に対して描き、
# z成分(高さ)には一切手を加えない。

potteryFocalPointBlock = defineBlock("PotteryFocalPoint")
defineInputs(potteryFocalPointBlock,
            "t",
            "point",
            "intensity",
            "circleRadius",
            "drawFrequency")

defineBlockInputDefaultValue(potteryFocalPointBlock.intensity, (0.0, 0.0, 0.0))
setMetaData(potteryFocalPointBlock.intensity, "Type", "Scalar")

defineBlockInputDefaultValue(potteryFocalPointBlock.circleRadius, (0.005, 0.0, 0.0))
setMetaData(potteryFocalPointBlock.circleRadius, "Type", "Scalar")

defineBlockInputDefaultValue(potteryFocalPointBlock.drawFrequency, (70.0, 0.0, 0.0))
setMetaData(potteryFocalPointBlock.drawFrequency, "Type", "Scalar")

def emitFocalPoint(inputs):
    time = inputs[0][0]
    point = inputs[1]
    intensity = inputs[2][0]
    circleRadius = inputs[3][0]
    drawFrequency = inputs[4][0]

    # 指位置(point)を中心に、半径circleRadiusの円をdrawFrequency[Hz]で描き続ける。
    # 高さ成分(z)には触れず、x・y成分だけを円運動させる。
    #
    # 【2026-09 追加修正】円の半径を、intensityに応じてスケールさせる
    # (effectiveRadius = circleRadius * intensity)。
    # 当初は「円の大きさは常に一定、intensityだけが変わる」という設計だったが、
    # これだと円運動という刺激そのものがintensityとは独立して常に一定の強さで
    # 感じられてしまい、intensityの微妙な差(±15%・±30%)が円運動の刺激に
    # 埋もれて弁別しにくいという問題があった。
    # 「弱い試行は小さく動く、強い試行は大きく動く」という形にすることで、
    # 動きの大きさそのものが強さの違いを直接表現するようにした。
    effectiveRadius = circleRadius * intensity

    angle = 2.0 * math.pi * time * drawFrequency
    offsetX = effectiveRadius * math.cos(angle)
    offsetY = effectiveRadius * math.sin(angle)

    movedPoint = (point[0] + offsetX, point[1] + offsetY, point[2])

    return (movedPoint[0], movedPoint[1], movedPoint[2], intensity)

defineOutputs(potteryFocalPointBlock, "out")
defineBlockOutputBehaviour(potteryFocalPointBlock.out, emitFocalPoint)
setMetaData(potteryFocalPointBlock.out, "Sensation-Producing", True)

attachDocumentation(potteryFocalPointBlock,
                    """
                    指位置を中心に、小さな円軌道を高速(drawFrequency, デフォルト70Hz)で
                    描き続けることで触覚を提示するSensation(空間時間変調・STM)。
                    point: (x, y, z) - エミッタ空間での中心位置(メートル単位)。
                    intensity: 0.0〜1.0を想定。強さは基本的に固定でよい(STMが刺激の主体のため)。
                    circleRadius: 円の半径[m]。デフォルト0.005(0.5cm)。
                    drawFrequency: 円を描く速さ[Hz]。デフォルト70Hz。
                    """)
