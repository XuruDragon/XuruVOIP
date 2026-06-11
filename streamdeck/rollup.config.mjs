import commonjs from "@rollup/plugin-commonjs";
import nodeResolve from "@rollup/plugin-node-resolve";
import terser from "@rollup/plugin-terser";
import typescript from "@rollup/plugin-typescript";

const isWatching = !!process.env.ROLLUP_WATCH;
const sdPlugin = "com.xurudragon.xuruvoip.sdPlugin";

/** @type {import('rollup').RollupOptions} */
const config = {
  input: "src/plugin.ts",
  output: {
    file: `${sdPlugin}/bin/plugin.js`,
    format: "esm",
    sourcemap: isWatching,
    sourcemapExcludeSources: true,
  },
  plugins: [
    typescript({
      mapRoot: isWatching ? "." : undefined,
    }),
    nodeResolve({
      browser: false,
      exportConditions: ["node"],
      preferBuiltins: true,
    }),
    commonjs(),
    !isWatching && terser(),
  ],
};

export default config;
