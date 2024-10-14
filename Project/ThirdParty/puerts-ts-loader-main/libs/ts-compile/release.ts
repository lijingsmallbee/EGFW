import { readFileSync } from "fs";
import { join } from "path";
import * as ts from "typescript";
import { hostGetDefaultLibMixin } from "./base";

function compile(saveTo: string, refs: string[], outputRelativePathCallback: (index: number) => string): boolean {
    let tsconfigIndex = 0;
    const errMsgs: string[] = [];
    
    const host = ts.createSolutionBuilderHost(Object.assign({}, ts.sys, {
        readFile(path: string) {
            if (path == '/puer-mock/tsconfig.json') {
                return JSON.stringify({ references: refs.map(item => ({ path: item })) })
            }
            return readFileSync(path, 'utf-8');
            // },
            // writeFile(...args: any[]) {
            //     if (!args[0].endsWith('tsconfig.tsbuildinfo')) {
            //         console.log(args[0])
            //         console.log(args)
            //     }
            //     return (ts.sys.writeFile as any).apply(ts.sys, args);
        }
    }), function (...args) {
        const config = args[1];
        
        if (config) {
            config.outDir = join(saveTo, outputRelativePathCallback(tsconfigIndex++));
            config.module = ts.ModuleKind.ES2015
        }
        return ts.createEmitAndSemanticDiagnosticsBuilderProgram.apply(ts, args);
    }, function (err) {
        if (typeof err.messageText == 'string')
            console.warn(err.messageText) 
        else 
            console.warn(err.messageText.messageText) 
    });
    Object.assign(host, hostGetDefaultLibMixin);
    
    const builder = ts.createSolutionBuilder(
        host,
        ["/puer-mock/tsconfig.json"], {}
    );
    const res = builder.build() == 0
    if (!res) errMsgs.forEach(console.error)
    else errMsgs.forEach(console.warn);

    return res;
}


export default function releaseTS(saveTo: string, tsConfigBasePaths: string[], outputRelativePathCallback: (index: number) => string = ((index) => index.toString())): boolean {
    return compile(saveTo, tsConfigBasePaths, outputRelativePathCallback);
}