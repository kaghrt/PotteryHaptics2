from pysensationcore import *

import math

# PotteryHaptics用の最小Sensation Block。
# 単一の集束点(point)を、可変の強さ(intensity)で出力する。
#
# 【重要】超音波の触覚は、ただ一定の強さで点を出すだけでは皮膚で感知できない。
# 実際に感じられるようにするには、強さを高周波(概ね100〜300Hz)で変調(振動)させる必要がある。
# ここでは140Hz前後で強度を振動させることで、皮膚に触覚として感じられるようにしている。
# (参考: 同梱のIntensityModulation.pyブロックと同じ考え方)

potteryFocalPointBlock = defineBlock("PotteryFocalPoint")
defineInputs(potteryFocalPointBlock,
            "t",
            "point",
            "intensity",
            "modulationFrequency")

defineBlockInputDefaultValue(potteryFocalPointBlock.intensity, (0.0, 0.0, 0.0))
setMetaData(potteryFocalPointBlock.intensity, "Type", "Scalar")

defineBlockInputDefaultValue(potteryFocalPointBlock.modulationFrequency, (140.0, 0.0, 0.0))
setMetaData(potteryFocalPointBlock.modulationFrequency, "Type", "Scalar")

def emitFocalPoint(inputs):
    time = inputs[0][0]
    point = inputs[1]
    baseIntensity = inputs[2][0]
    modulationFrequency = inputs[3][0]

    # 0〜baseIntensityの範囲で高速に振動させることで、皮膚で感知できる触覚にする
    modulated = baseIntensity * 0.5 * (1 - math.cos(2 * math.pi * time * modulationFrequency))

    return (point[0], point[1], point[2], modulated)

defineOutputs(potteryFocalPointBlock, "out")
defineBlockOutputBehaviour(potteryFocalPointBlock.out, emitFocalPoint)
setMetaData(potteryFocalPointBlock.out, "Sensation-Producing", True)

attachDocumentation(potteryFocalPointBlock,
                    """
                    単一の集束点を、高周波(modulationFrequency, デフォルト140Hz)で
                    強度変調しながら出力するSensation。
                    point: (x, y, z) - エミッタ空間での位置(メートル単位)
                    intensity: 0.0〜1.0を想定。変調前の基準強度。
                    modulationFrequency: 強度を振動させる周波数[Hz]。触覚として感知しやすい100〜300Hz程度を推奨。
                    """)
