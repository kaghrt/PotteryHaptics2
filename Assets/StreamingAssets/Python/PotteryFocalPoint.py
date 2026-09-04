from pysensationcore import *

import math

# PotteryHaptics用の最小Sensation Block。
# 単一の集束点(point)を、可変の強さ(intensity)で出力する。
#
# 【2026-09 修正】当初は (x, y, z, intensity) のタプルを自前で組み立てて返していたが、
# Ultraleap公式デモ(Demo Suite)のソースを確認したところ、公式の実装は
# 「位置」と「強さ」を別々に SensationCore 内蔵の SetIntensity ブロックへ渡し、
# そこで正しく合成させる設計になっていた。
# 自前でタプルを組み立てる方式は動作はするが、SetIntensityが内部で行っている
# 正規化・スケーリング処理を経由しないため、実機での出力が弱くなっていた
# 可能性がある。今回、公式と同じSetIntensity経由の構成に修正した。
#
# 強度変調(AM)自体は、公式のIntensityModulation.pyと同じ式・同程度の周波数を採用。

potteryFocalPointBlock = defineBlock("PotteryFocalPoint")
defineInputs(potteryFocalPointBlock,
            "t",
            "point",
            "intensity",
            "modulationFrequency")

defineBlockInputDefaultValue(potteryFocalPointBlock.intensity, (0.0, 0.0, 0.0))
setMetaData(potteryFocalPointBlock.intensity, "Type", "Scalar")

defineBlockInputDefaultValue(potteryFocalPointBlock.modulationFrequency, (143.0, 0.0, 0.0))
setMetaData(potteryFocalPointBlock.modulationFrequency, "Type", "Scalar")

def modulateIntensity(inputs):
    time = inputs[0][0]
    baseIntensity = inputs[1][0]
    modulationFrequency = inputs[2][0]

    # 公式IntensityModulation.pyと同じ式(振幅変調)。baseIntensityで全体の強さをスケールする。
    modulated = baseIntensity * 0.5 * (1 - math.cos(2 * math.pi * time * modulationFrequency))
    return (modulated, 0.0, 0.0)

modulationOutputBlock = defineBlock("PotteryIntensityModulation")
defineInputs(modulationOutputBlock, "t", "baseIntensity", "modulationFrequency")
defineOutputs(modulationOutputBlock, "out")
defineBlockOutputBehaviour(modulationOutputBlock.out, modulateIntensity)
setMetaData(modulationOutputBlock.out, "Sensation-Producing", False)

modulationInstance = createInstance("PotteryIntensityModulation", "PotteryIntensityModulationInstance")
connect(potteryFocalPointBlock.t, modulationInstance.t)
connect(potteryFocalPointBlock.intensity, modulationInstance.baseIntensity)
connect(potteryFocalPointBlock.modulationFrequency, modulationInstance.modulationFrequency)

# 公式と同じく、SensationCore内蔵のSetIntensityブロックで位置と強さを合成する。
setIntensityInstance = createInstance("SetIntensity", "PotteryFocalPointSetIntensityInstance")
connect(potteryFocalPointBlock.point, setIntensityInstance.point)
connect(modulationInstance.out, setIntensityInstance.intensity)

defineOutputs(potteryFocalPointBlock, "out")
connect(setIntensityInstance.out, potteryFocalPointBlock.out)
setMetaData(potteryFocalPointBlock.out, "Sensation-Producing", True)

attachDocumentation(potteryFocalPointBlock,
                    """
                    単一の集束点を、高周波(modulationFrequency, デフォルト143Hz)で
                    強度変調しながら出力するSensation。
                    位置と強さの合成には、公式デモと同じくSensationCore内蔵の
                    SetIntensityブロックを使用している。
                    point: (x, y, z) - エミッタ空間での位置(メートル単位)
                    intensity: 0.0〜1.0を想定。変調前の基準強度。
                    modulationFrequency: 強度を振動させる周波数[Hz]。
                    """)
