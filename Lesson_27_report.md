# Отчет о расследовании зависания приложения (deadlock)

## В чем заключалась проблема

Приложение зависло из-за **взаимной блокировки (deadlock)**.

## Все потоки: `!threads`

ThreadCount:      12
UnstartedThread:  0
BackgroundThread: 4
PendingThread:    0
DeadThread:       5
Hosted Runtime:   no
Lock
DBG   ID     OSID ThreadOBJ           State GC Mode     GC Alloc Context                  Domain           Lock Count Apt Exception
0    1     5ab8 000001E982907560  202a020 Preemptive  000001E987115A40:000001E987117030 000001e9828a56b0 -00001 MTA
7    2     7ee0 000001E9828B8B20    2b220 Preemptive  0000000000000000:0000000000000000 000001e9828a56b0 -00001 MTA (Finalizer)
9    4     5ff8 000001E980605940  202b220 Preemptive  000001E9871802A8:000001E987180FD0 000001e9828a56b0 -00001 MTA
10    5     5898 000001E9805D3180  102b220 Preemptive  000001E9870D04D8:000001E9870D0FD0 000001e9828a56b0 -00001 MTA (Threadpool Worker)
XXXX    6        0 000001E980635FF0  1039820 Preemptive  0000000000000000:0000000000000000 000001e9828a56b0 -00001 Ukn (Threadpool Worker)
11    7     7d20 000001E980647B60  302b220 Preemptive  000001E9870D32B0:000001E9870D5010 000001e9828a56b0 -00001 MTA (Threadpool Worker)
XXXX    8        0 000001E98066C410  1039820 Preemptive  0000000000000000:0000000000000000 000001e9828a56b0 -00001 Ukn (Threadpool Worker)
XXXX    9        0 000001E98066B910  1039820 Preemptive  0000000000000000:0000000000000000 000001e9828a56b0 -00001 Ukn (Threadpool Worker)
XXXX   10        0 000001E980672B10  1039820 Preemptive  0000000000000000:0000000000000000 000001e9828a56b0 -00001 Ukn (Threadpool Worker)
XXXX   11        0 000001E9806A13E0  1039820 Preemptive  0000000000000000:0000000000000000 000001e9828a56b0 -00001 Ukn (Threadpool Worker)
13   12      884 000001E9806C2470  202b020 Preemptive  000001E987117078:000001E987119050 000001e9828a56b0 -00001 MTA
14   13      e68 000001E9806C3A70  202b020 Preemptive  0000000000000000:0000000000000000 000001e9828a56b0 -00001 MTA

## Потоки, вовлечённые в проблему: `!syncblk`

Index         SyncBlock MonitorHeld Recursion Owning Thread Info          SyncBlock Owner
3 000001E98069E2A8            3         1 000001E9806C3A70 e68  14   000001e987117060 System.Object
4 000001E98069E300            3         1 000001E9806C2470 884  13   000001e987117048 System.Object

ID потоков из `!threads`: **DBG 13** и **DBG 14** участвуют во взаимоблокировке (дополнительно указывает  `MonitorHeld = 3`).

## Стеки вызовов потоков

OS Thread Id: 0x884 (13)
Child SP               IP Call Site
0000001407F7F7F8 00007ffa1f761914 [HelperMethodFrame_1OBJ: 0000001407f7f7f8] System.Threading.Monitor.ReliableEnter(System.Object, Boolean ByRef)
0000001407F7F940 00007ff854ee3749
LOCALS:
0x0000001407F7F988 = 0x000001e987117048
0x0000001407F7F980 = 0x0000000000000001
0x0000001407F7F978 = 0x000001e987117060
0x0000001407F7F970 = 0x0000000000000000

0000001407F7FBD8 00007ff8b4a5f123 [DebuggerU2MCatchHandlerFrame: 0000001407f7fbd8]

OS Thread Id: 0xe68 (14)
Child SP               IP Call Site
00000014080FF3C8 00007ffa1f761914 [HelperMethodFrame_1OBJ: 00000014080ff3c8] System.Threading.Monitor.ReliableEnter(System.Object, Boolean ByRef)
00000014080FF510 00007ff854ee3959
LOCALS:
0x00000014080FF558 = 0x000001e987117060
0x00000014080FF550 = 0x0000000000000001
0x00000014080FF548 = 0x000001e987117048
0x00000014080FF540 = 0x0000000000000000

00000014080FF7A8 00007ff8b4a5f123 [DebuggerU2MCatchHandlerFrame: 00000014080ff7a8]

В LOCALS каждого потока видны оба адреса (`…048` и `…060`) — оба lock-объекта находятся в области видимости метода. 

## Объекты, на которые применён примитив lock

`!dumpobj 000001e987117060`
Name:        System.Object
MethodTable: 00007ff854e05fa8
EEClass:     00007ff854dff668
Tracked Type: false
Size:        24(0x18) bytes
File:        C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.30\System.Private.CoreLib.dll
Object
Fields:
None

`!dumpobj 000001e987117048`
Name:        System.Object
MethodTable: 00007ff854e05fa8
EEClass:     00007ff854dff668
Tracked Type: false
Size:        24(0x18) bytes
File:        C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.30\System.Private.CoreLib.dll
Object
Fields:
None

Оба объекта — простые `System.Object` (_lock = new object())

## Проблемный метод

`!ip2md 00007ff854ee3959` (адрес из стека потока 14):
MethodDesc:   00007ff85521cae8
Method Name:          ConsoleApp1.Process.Method2()
Class:                00007ff855230710
MethodTable:          00007ff85521cb10
mdToken:              0000000006000004
Module:               00007ff854f5e0a0
IsJitted:             yes
Current CodeAddr:     00007ff854ee38b0
Version History:
ILCodeVersion:      0000000000000000
ReJIT ID:           0
IL Addr:            000001e984412150
CodeAddr:           00007ff854ee38b0  (MinOptJitted)
NativeCodeVersion:  0000000000000000

## Вывод

**Проблема:** взаимоблокировка (deadlock) — циклическое ожидание двух потоков.